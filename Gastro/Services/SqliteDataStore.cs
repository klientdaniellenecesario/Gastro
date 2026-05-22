using System.Security.Cryptography;
using GastroCebu.Data;
using GastroCebu.Models;
using Microsoft.EntityFrameworkCore;

namespace GastroCebu.Services;

public class SqliteDataStore(IDbContextFactory<TasteCebuDbContext> factory, IConfiguration config)
{
    private readonly IDbContextFactory<TasteCebuDbContext> _factory = factory;
    private readonly IConfiguration _config = config;

    public void Initialize()
    {
        using var db = _factory.CreateDbContext();
        db.Database.EnsureCreated();
        Seed(db);
    }

    public AppUser? FindUserByEmail(string email)
    {
        using var db = _factory.CreateDbContext();
        return db.Users.FirstOrDefault(u => u.Email == email);
    }

    public AppUser? FindUserById(int id)
    {
        using var db = _factory.CreateDbContext();
        return db.Users.Find(id);
    }

    public List<AppUser> GetUsers()
    {
        using var db = _factory.CreateDbContext();
        return [.. db.Users.OrderByDescending(u => u.CreatedAt)];
    }

    public AppUser CreateUser(string fullName, string email, string password, string role = "User")
    {
        using var db = _factory.CreateDbContext();
        var user = new AppUser
        {
            FullName = fullName,
            Email = email,
            PasswordHash = HashPassword(password),
            Role = role,
            Location = "Cebu City, Philippines",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        db.SaveChanges();
        AddActivity(user.Id, "Created an account");
        return user;
    }

    public void UpdateUser(int userId, string fullName, string location)
    {
        using var db = _factory.CreateDbContext();
        var user = db.Users.Find(userId);
        if (user is null) return;
        user.FullName = fullName;
        user.Location = location;
        db.SaveChanges();
        AddActivity(userId, "Updated profile");
    }

    public void ChangePassword(int userId, string password)
    {
        using var db = _factory.CreateDbContext();
        var user = db.Users.Find(userId);
        if (user is null) return;
        user.PasswordHash = HashPassword(password);
        db.SaveChanges();
        AddActivity(userId, "Changed password");
    }

    public void AdminUpdateUser(int userId, string fullName, string email)
    {
        using var db = _factory.CreateDbContext();
        var user = db.Users.Find(userId);
        if (user is null) return;
        user.FullName = fullName;
        user.Email = email;
        db.SaveChanges();
    }

    public void SetAdminRole(int userId, bool isAdmin)
    {
        using var db = _factory.CreateDbContext();
        var user = db.Users.Find(userId);
        if (user is null) return;
        user.Role = isAdmin ? "Admin" : "User";
        db.SaveChanges();
    }

    public List<RestaurantListing> GetRestaurants()
    {
        using var db = _factory.CreateDbContext();
        return [.. db.Restaurants.OrderByDescending(r => r.CreatedAt)];
    }

    public void AddRestaurant(string name, string photoUrl, string description, string address = "Cebu", string category = "Restaurant")
    {
        using var db = _factory.CreateDbContext();
        db.Restaurants.Add(new RestaurantListing
        {
            Name = name,
            Address = address,
            Category = category,
            PhotoUrl = photoUrl,
            Description = description,
            Rating = 0,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    public List<DishListing> GetDishes()
    {
        using var db = _factory.CreateDbContext();
        return [.. db.Dishes.OrderByDescending(d => d.Id)];
    }

    public List<RestaurantListing> SearchRestaurants(string query = "", string category = "", decimal minRating = 0, string sortBy = "newest")
    {
        using var db = _factory.CreateDbContext();
        var results = db.Restaurants.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.ToLowerInvariant();
            results = results.Where(r =>
                r.Name.ToLower().Contains(q) ||
                r.Address.ToLower().Contains(q) ||
                r.Description.ToLower().Contains(q) ||
                r.Category.ToLower().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(category) && category.ToLowerInvariant() != "all")
            results = results.Where(r => r.Category.ToLowerInvariant() == category.ToLowerInvariant());

        if (minRating > 0)
            results = results.Where(r => r.Rating >= minRating);

        results = sortBy?.ToLowerInvariant() switch
        {
            "rating" => results.OrderByDescending(r => r.Rating),
            "name" => results.OrderBy(r => r.Name),
            "newest" or _ => results.OrderByDescending(r => r.CreatedAt)
        };

        return [.. results];
    }

    public List<RestaurantListing> GetRestaurantsByCategory(string category)
    {
        using var db = _factory.CreateDbContext();
        return [.. db.Restaurants
            .Where(r => r.Category.ToLower() == category.ToLower())
            .OrderByDescending(r => r.Rating)];
    }

    public List<RestaurantListing> GetRestaurantsByRating(decimal minRating = 4.0m)
    {
        using var db = _factory.CreateDbContext();
        return [.. db.Restaurants
            .Where(r => r.Rating >= minRating)
            .OrderByDescending(r => r.Rating)];
    }

    public List<DishListing> SearchDishes(string query = "", string tags = "", string sortBy = "newest")
    {
        using var db = _factory.CreateDbContext();
        var results = db.Dishes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.ToLowerInvariant();
            results = results.Where(d =>
                d.Name.ToLower().Contains(q) ||
                d.Description.ToLower().Contains(q) ||
                d.Tags.ToLower().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(tags))
        {
            var tagArray = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .ToList();
            results = results.Where(d => tagArray.Any(tag => d.Tags.ToLower().Contains(tag)));
        }

        results = sortBy?.ToLowerInvariant() switch
        {
            "trending" => results.Where(d => d.IsTrending).OrderByDescending(d => d.Id),
            "new" => results.Where(d => d.IsNewThisMonth).OrderByDescending(d => d.Id),
            "price-low" => results.OrderBy(d => d.Price),
            "price-high" => results.OrderByDescending(d => d.Price),
            "newest" or _ => results.OrderByDescending(d => d.Id)
        };

        return [.. results];
    }

    public List<DishListing> GetTrendingDishes()
    {
        using var db = _factory.CreateDbContext();
        return [.. db.Dishes.Where(d => d.IsTrending).OrderByDescending(d => d.Id)];
    }

    public List<DishListing> GetNewDishes()
    {
        using var db = _factory.CreateDbContext();
        return [.. db.Dishes.Where(d => d.IsNewThisMonth).OrderByDescending(d => d.Id)];
    }

    public void AddDish(string name, string photoUrl, string description, decimal price = 0, string tags = "", bool isNew = false, bool isTrending = false, int? restaurantId = null)
    {
        using var db = _factory.CreateDbContext();
        db.Dishes.Add(new DishListing
        {
            Name = name,
            Price = price,
            PhotoUrl = photoUrl,
            Description = description,
            Tags = tags,
            IsNewThisMonth = isNew,
            IsTrending = isTrending,
            RestaurantId = restaurantId
        });
        db.SaveChanges();
    }

    public List<FoodEvent> GetEvents()
    {
        using var db = _factory.CreateDbContext();
        return [.. db.Events.OrderBy(e => e.Date)];
    }

    public void AddEvent(string title, string photoUrl, string description, DateTime? date = null, string location = "Cebu City", int availableSlots = 20)
    {
        using var db = _factory.CreateDbContext();
        db.Events.Add(new FoodEvent
        {
            Title = title,
            Date = date ?? DateTime.UtcNow.AddDays(30),
            Location = location,
            TotalSlots = availableSlots,
            AvailableSlots = availableSlots,
            PhotoUrl = photoUrl,
            Description = description
        });
        db.SaveChanges();
    }

    public List<ReviewEntry> GetAllReviews()
    {
        using var db = _factory.CreateDbContext();
        return [.. db.Reviews.OrderByDescending(r => r.CreatedAt)];
    }

    public List<ReviewEntry> GetUserReviews(int userId)
    {
        using var db = _factory.CreateDbContext();
        return [.. db.Reviews.Where(r => r.UserId == userId).OrderByDescending(r => r.CreatedAt)];
    }

    public Dictionary<int, string> ResolveReviewNames(List<ReviewEntry> reviews)
    {
        using var db = _factory.CreateDbContext();
        var result = new Dictionary<int, string>();
        foreach (var r in reviews)
        {
            result[r.Id] = r.TargetType == "restaurant"
                ? db.Restaurants.Find(r.TargetId)?.Name ?? $"Restaurant #{r.TargetId}"
                : r.TargetType == "dish"
                    ? db.Dishes.Find(r.TargetId)?.Name ?? $"Dish #{r.TargetId}"
                    : $"{r.TargetType} #{r.TargetId}";
        }
        return result;
    }

    public void AddReview(int userId, string targetType, int targetId, int rating, string text)
    {
        using var db = _factory.CreateDbContext();
        db.Reviews.Add(new ReviewEntry
        {
            UserId = userId,
            TargetType = targetType,
            TargetId = targetId,
            Rating = rating,
            Text = text,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        if (targetType == "restaurant") RecalcRestaurantRating(db, targetId);
        AddActivity(userId, $"Reviewed a {targetType}");
    }

    public bool UpdateReview(int userId, int reviewId, int rating, string text)
    {
        using var db = _factory.CreateDbContext();
        var review = db.Reviews.FirstOrDefault(r => r.Id == reviewId && r.UserId == userId);
        if (review is null) return false;
        review.Rating = rating;
        review.Text = text;
        db.SaveChanges();
        if (review.TargetType == "restaurant") RecalcRestaurantRating(db, review.TargetId);
        AddActivity(userId, "Edited a review");
        return true;
    }

    public void DeleteReview(int userId, int reviewId, bool isAdmin)
    {
        using var db = _factory.CreateDbContext();
        var review = isAdmin
            ? db.Reviews.Find(reviewId)
            : db.Reviews.FirstOrDefault(r => r.Id == reviewId && r.UserId == userId);
        if (review is null) return;
        var targetType = review.TargetType;
        var targetId = review.TargetId;
        db.Reviews.Remove(review);
        db.SaveChanges();
        if (targetType == "restaurant") RecalcRestaurantRating(db, targetId);
        AddActivity(userId, "Deleted a review");
    }

    private static void RecalcRestaurantRating(TasteCebuDbContext db, int restaurantId)
    {
        var restaurant = db.Restaurants.Find(restaurantId);
        if (restaurant is null) return;
        var avg = db.Reviews
            .Where(r => r.TargetType == "restaurant" && r.TargetId == restaurantId)
            .Average(r => (decimal?)r.Rating) ?? 0m;
        restaurant.Rating = Math.Round(avg, 1);
        db.SaveChanges();
    }

    public List<BookmarkEntry> GetBookmarks(int userId, string? type = null)
    {
        using var db = _factory.CreateDbContext();
        var q = db.Bookmarks.Where(b => b.UserId == userId);
        if (type is not null) q = q.Where(b => b.ItemType == type);
        return [.. q.OrderByDescending(b => b.CreatedAt)];
    }

    public void ToggleBookmark(int userId, string itemType, int itemId, string itemName)
    {
        using var db = _factory.CreateDbContext();
        var existing = db.Bookmarks
            .FirstOrDefault(b => b.UserId == userId && b.ItemType == itemType && b.ItemId == itemId);
        if (existing is not null)
        {
            db.Bookmarks.Remove(existing);
            db.SaveChanges();
            AddActivity(userId, $"Removed {itemName} from bookmarks");
            return;
        }
        db.Bookmarks.Add(new BookmarkEntry
        {
            UserId = userId,
            ItemType = itemType,
            ItemId = itemId,
            ItemName = itemName,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        AddActivity(userId, $"Saved {itemName}");
    }

    public List<EventRegistration> GetRegistrations(int userId)
    {
        using var db = _factory.CreateDbContext();
        return [.. db.EventRegistrations.Where(r => r.UserId == userId).OrderByDescending(r => r.RegisteredAt)];
    }

    public void RegisterForEvent(int userId, int eventId)
    {
        using var db = _factory.CreateDbContext();
        using var tx = db.Database.BeginTransaction();
        var ev = db.Events.Find(eventId)
                 ?? throw new InvalidOperationException("Event not found.");
        if (ev.AvailableSlots <= 0)
            throw new InvalidOperationException("No seats left.");
        var alreadyRegistered = db.EventRegistrations.Any(r => r.UserId == userId && r.EventId == eventId);
        if (!alreadyRegistered)
        {
            db.EventRegistrations.Add(new EventRegistration
            {
                UserId = userId,
                EventId = eventId,
                EventTitle = ev.Title,
                RegisteredAt = DateTime.UtcNow
            });
            ev.AvailableSlots--;
            db.SaveChanges();
            tx.Commit();
            AddActivity(userId, $"Registered for {ev.Title}");
        }
    }

    public void CancelEventRegistration(int userId, int eventId)
    {
        using var db = _factory.CreateDbContext();
        using var tx = db.Database.BeginTransaction();
        var reg = db.EventRegistrations.FirstOrDefault(r => r.UserId == userId && r.EventId == eventId);
        if (reg is null) return;
        db.EventRegistrations.Remove(reg);
        var ev = db.Events.Find(eventId);
        if (ev is not null) ev.AvailableSlots++;
        db.SaveChanges();
        tx.Commit();
        AddActivity(userId, "Cancelled an event registration");
    }

    public List<ActivityEntry> GetActivities(int userId)
    {
        using var db = _factory.CreateDbContext();
        return [.. db.Activities.Where(a => a.UserId == userId).OrderByDescending(a => a.CreatedAt).Take(20)];
    }

    public List<ActivityEntry> GetRecentActivities(int take = 10)
    {
        using var db = _factory.CreateDbContext();
        return [.. db.Activities.OrderByDescending(a => a.CreatedAt).Take(take)];
    }

    public List<AppUser> GetEventRegistrants(int eventId)
    {
        using var db = _factory.CreateDbContext();
        var userIds = db.EventRegistrations
            .Where(r => r.EventId == eventId)
            .Select(r => r.UserId)
            .ToList();
        return [.. db.Users.Where(u => userIds.Contains(u.Id)).OrderBy(u => u.FullName)];
    }

    public Dictionary<int, string> GetUserNames(List<ReviewEntry> reviews)
    {
        using var db = _factory.CreateDbContext();
        var userIds = reviews.Select(r => r.UserId).Distinct().ToList();
        return db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionary(u => u.Id, u => u.FullName);
    }

    public void AddActivity(int userId, string message)
    {
        using var db = _factory.CreateDbContext();
        db.Activities.Add(new ActivityEntry
        {
            UserId = userId,
            Message = message,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    public void UpdateRestaurant(int id, string name, string address, string category, string photoUrl, string description)
    {
        using var db = _factory.CreateDbContext();
        var r = db.Restaurants.Find(id);
        if (r is null) return;
        r.Name = name;
        r.Address = address;
        r.Category = category;
        if (!string.IsNullOrWhiteSpace(photoUrl)) r.PhotoUrl = photoUrl;
        r.Description = description;
        db.SaveChanges();
    }

    public void UpdateDish(int id, string name, decimal price, string photoUrl, string description, string tags, bool isNew, bool isTrending, int? restaurantId = null)
    {
        using var db = _factory.CreateDbContext();
        var d = db.Dishes.Find(id);
        if (d is null) return;
        d.Name = name;
        d.Price = price;
        if (!string.IsNullOrWhiteSpace(photoUrl)) d.PhotoUrl = photoUrl;
        d.Description = description;
        d.Tags = tags;
        d.IsNewThisMonth = isNew;
        d.IsTrending = isTrending;
        if (restaurantId.HasValue) d.RestaurantId = restaurantId;
        db.SaveChanges();
    }

    public void UpdateEvent(int id, string title, DateTime date, string location, int availableSlots, string photoUrl, string description)
    {
        using var db = _factory.CreateDbContext();
        var e = db.Events.Find(id);
        if (e is null) return;
        e.Title = title;
        e.Date = date;
        e.Location = location;
        e.AvailableSlots = availableSlots;
        if (!string.IsNullOrWhiteSpace(photoUrl)) e.PhotoUrl = photoUrl;
        e.Description = description;
        db.SaveChanges();
    }

    public void DeleteFromAdmin(string type, int id)
    {
        using var db = _factory.CreateDbContext();
        switch (type)
        {
            case "restaurant":
                var r = db.Restaurants.Find(id);
                if (r is not null) db.Restaurants.Remove(r);
                break;
            case "dish":
                var d = db.Dishes.Find(id);
                if (d is not null) db.Dishes.Remove(d);
                break;
            case "event":
                var e = db.Events.Find(id);
                if (e is not null) db.Events.Remove(e);
                break;
            case "review":
                var rv = db.Reviews.Find(id);
                if (rv is not null) db.Reviews.Remove(rv);
                break;
            case "user":
                var u = db.Users.Find(id);
                if (u is not null) db.Users.Remove(u);
                break;
            default:
                throw new InvalidOperationException("Invalid admin item type.");
        }
        db.SaveChanges();
    }

    public int Count(string table)
    {
        using var db = _factory.CreateDbContext();
        return table switch
        {
            "Users" => db.Users.Count(),
            "Restaurants" => db.Restaurants.Count(),
            "Dishes" => db.Dishes.Count(),
            "Events" => db.Events.Count(),
            "Reviews" => db.Reviews.Count(),
            _ => 0
        };
    }

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private void Seed(TasteCebuDbContext db)
    {
        if (!db.Users.Any())
        {
            var adminPassword = _config["AdminSeed:Password"] ?? "ChangeMe123!";
            CreateUser("TasteCebu Admin", "admin@tastecebu.test", adminPassword, "Admin");
        }

        if (!db.Restaurants.Any())
        {
            var restaurants = new List<RestaurantListing>
            {
                new() {
                    Name = "House of Lechon",
                    Address = "Capitol Site, Cebu City",
                    Category = "Restaurant",
                    PhotoUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=600&h=400&fit=crop",
                    Description = "Authentic Cebu lechon with crispy skin and signature herbs. The go-to spot for the best lechon in the country.",
                    Rating = 4.9m,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new() {
                    Name = "Pig and Palm",
                    Address = "Nivel Hills, Lahug, Cebu City",
                    Category = "Restaurant",
                    PhotoUrl = "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=600&h=400&fit=crop",
                    Description = "Modern European cuisine with Cebuano soul. Craft cocktails, stunning sunset views, and an unforgettable dining experience.",
                    Rating = 4.9m,
                    CreatedAt = DateTime.UtcNow.AddDays(-25)
                },
                new() {
                    Name = "Good Cup Coffee",
                    Address = "AS Fortuna St, Mandaue City",
                    Category = "Cafe",
                    PhotoUrl = "https://images.unsplash.com/photo-1559339352-11d035aa65de?w=600&h=400&fit=crop",
                    Description = "Single-origin brews, minimalist industrial vibe, and award-winning pastries. Cebu's favorite specialty coffee shop.",
                    Rating = 4.8m,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },
                new() {
                    Name = "Lantaw Native Restaurant",
                    Address = "Cordova, Cebu",
                    Category = "Restaurant",
                    PhotoUrl = "https://images.unsplash.com/photo-1537047902294-62a40c20a6ae?w=600&h=400&fit=crop",
                    Description = "Floating cottages over the sea, fresh seafood, and spectacular sunset views. A truly unique Cebu dining experience.",
                    Rating = 4.7m,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new() {
                    Name = "STK ta Bay! Sa Sugbo Mercado",
                    Address = "Sugbo Mercado, IT Park, Cebu City",
                    Category = "Street Food",
                    PhotoUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600&h=400&fit=crop",
                    Description = "Famous for their grilled chicken inasal and puso. A must-visit at the iconic Sugbo Mercado night market.",
                    Rating = 4.6m,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new() {
                    Name = "The Patio at Casa Gorordo",
                    Address = "Lopez Jaena St, Cebu City",
                    Category = "Cafe",
                    PhotoUrl = "https://images.unsplash.com/photo-1554118811-1e0d58224f24?w=600&h=400&fit=crop",
                    Description = "Heritage cafe inside a 19th-century Spanish colonial house. Known for their tsokolate and traditional Cebuano snacks.",
                    Rating = 4.7m,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new() {
                    Name = "Abaca Restaurant",
                    Address = "Mactan Island, Cebu",
                    Category = "Restaurant",
                    PhotoUrl = "https://images.unsplash.com/photo-1590577976322-3d2d6e2130d5?w=600&h=400&fit=crop",
                    Description = "Beachfront fine dining with Asian-Mediterranean fusion cuisine. One of the top restaurants in the Philippines.",
                    Rating = 4.8m,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new() {
                    Name = "Hukad",
                    Address = "SM City Cebu, North Reclamation Area",
                    Category = "Restaurant",
                    PhotoUrl = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=600&h=400&fit=crop",
                    Description = "Traditional Filipino comfort food with generous servings of kare-kare, sinigang, and lechon kawali.",
                    Rating = 4.5m,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
            };
            db.Restaurants.AddRange(restaurants);
            db.SaveChanges();
        }

        if (!db.Dishes.Any())
        {
            var dishes = new List<DishListing>
            {
                new() {
                    Name = "Cebu Lechon",
                    Price = 650,
                    PhotoUrl = "https://images.unsplash.com/photo-1569694119452-2b7eae2e4cbc?w=600&h=400&fit=crop",
                    Tags = "Pork, Roasted, Cebu Classic, Must-Try",
                    IsNewThisMonth = false,
                    IsTrending = true
                },
                new() {
                    Name = "Kinilaw na Tuna",
                    Price = 280,
                    PhotoUrl = "https://images.unsplash.com/photo-1615141982883-c7ad0a69fd62?w=600&h=400&fit=crop",
                    Tags = "Seafood, Raw, Spicy, Cebu Classic",
                    IsNewThisMonth = false,
                    IsTrending = true
                },
                new() {
                    Name = "Puso with Chicken Inasal",
                    Price = 120,
                    PhotoUrl = "https://images.unsplash.com/photo-1563805042-7684c019e1cb?w=600&h=400&fit=crop",
                    Tags = "Grilled, Chicken, Street Food, Budget",
                    IsNewThisMonth = false,
                    IsTrending = true
                },
                new() {
                    Name = "Sutukil Platter",
                    Price = 450,
                    PhotoUrl = "https://images.unsplash.com/photo-1615937722923-69f6dde0c6f3?w=600&h=400&fit=crop",
                    Tags = "Seafood, Grilled, Stewed, Cebu Classic",
                    IsNewThisMonth = false,
                    IsTrending = false
                },
                new() {
                    Name = "Balbacua",
                    Price = 180,
                    PhotoUrl = "https://images.unsplash.com/photo-1547592166-23ac45744acd?w=600&h=400&fit=crop",
                    Tags = "Beef, Stewed, Comfort Food, Local",
                    IsNewThisMonth = false,
                    IsTrending = false
                },
                new() {
                    Name = "Ube Champorado",
                    Price = 95,
                    PhotoUrl = "https://images.unsplash.com/photo-1580476262798-bddd9f4b7369?w=600&h=400&fit=crop",
                    Tags = "Dessert, Breakfast, Ube, Sweet",
                    IsNewThisMonth = true,
                    IsTrending = false
                },
                new() {
                    Name = "Lechon Ramen",
                    Price = 320,
                    PhotoUrl = "https://images.unsplash.com/photo-1569050467447-ce54b3bbc37d?w=600&h=400&fit=crop",
                    Tags = "Fusion, Ramen, Pork, Trending",
                    IsNewThisMonth = true,
                    IsTrending = true
                },
                new() {
                    Name = "Mango Float",
                    Price = 85,
                    PhotoUrl = "https://images.unsplash.com/photo-1488477181946-6428a0291777?w=600&h=400&fit=crop",
                    Tags = "Dessert, Mango, Sweet, Filipino",
                    IsNewThisMonth = true,
                    IsTrending = false
                },
            };
            db.Dishes.AddRange(dishes);
            db.SaveChanges();
        }

        if (!db.Events.Any())
        {
            var events = new List<FoodEvent>
            {
                new() {
                    Title = "Lechon Masterclass at Carbon Market",
                    Date = DateTime.UtcNow.AddDays(26),
                    Location = "Carbon Market, Cebu City",
                    TotalSlots = 15,
                    AvailableSlots = 15,
                    PhotoUrl = "https://images.unsplash.com/photo-1556910104-525b138803e9?w=600&h=400&fit=crop",
                    Description = "Learn the secrets of Cebu's world-famous lechon from master roasters. Includes hands-on pig preparation and roasting."
                },
                new() {
                    Title = "Cebu Street Food Crawl",
                    Date = DateTime.UtcNow.AddDays(33),
                    Location = "Colon Street, Cebu City",
                    TotalSlots = 32,
                    AvailableSlots = 32,
                    PhotoUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600&h=400&fit=crop",
                    Description = "Explore the best street eats in Colon and Carbon Market with a local food guide. 10+ food stops included."
                },
                new() {
                    Title = "Coffee Cupping & Latte Art Workshop",
                    Date = DateTime.UtcNow.AddDays(41),
                    Location = "Good Cup Coffee, Mandaue",
                    TotalSlots = 20,
                    AvailableSlots = 20,
                    PhotoUrl = "https://images.unsplash.com/photo-1534087298023-017580581d8d?w=600&h=400&fit=crop",
                    Description = "Hands-on espresso training with Cebu's top baristas. Learn cupping, extraction, and latte art from scratch."
                },
                new() {
                    Title = "Kinilaw & Craft Beer Pairing",
                    Date = DateTime.UtcNow.AddDays(55),
                    Location = "Abaca Restaurant, Mactan",
                    TotalSlots = 25,
                    AvailableSlots = 25,
                    PhotoUrl = "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=600&h=400&fit=crop",
                    Description = "A guided pairing session featuring Cebu's finest kinilaw variations matched with local craft beers."
                },
            };
            db.Events.AddRange(events);
            db.SaveChanges();
        }
    }
}