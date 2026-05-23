namespace GastroCebu.Models;

public class GastroDatabase
{
    public List<AppUser> Users { get; set; } = [];
    public List<RestaurantListing> Restaurants { get; set; } = [];
    public List<DishListing> Dishes { get; set; } = [];
    public List<FoodEvent> Events { get; set; } = [];
    public List<ReviewEntry> Reviews { get; set; } = [];
    public List<BookmarkEntry> Bookmarks { get; set; } = [];
    public List<EventRegistration> EventRegistrations { get; set; } = [];
    public List<ActivityEntry> Activities { get; set; } = [];
}

public class AppUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User";
    public string Location { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RestaurantListing
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Category { get; set; } = "Restaurant";
    public string PhotoUrl { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Rating { get; set; }
    public string Vibe { get; set; } = "all";   // all | chill | romantic | fun | solo | family
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DishListing
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string PhotoUrl { get; set; } = "";
    public string Description { get; set; } = "";
    public string Tags { get; set; } = "";
    public bool IsNewThisMonth { get; set; }
    public bool IsTrending { get; set; }
    public int? RestaurantId { get; set; }
}

public class FoodEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime Date { get; set; }
    public string Location { get; set; } = "";
    public int TotalSlots { get; set; }      // NEW — set once on creation, never changes
    public int AvailableSlots { get; set; }  // decrements on register, increments on cancel
    public string PhotoUrl { get; set; } = "";
    public string Description { get; set; } = "";
}

public class ReviewEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TargetType { get; set; } = "restaurant";
    public int TargetId { get; set; }
    public int Rating { get; set; }
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class BookmarkEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ItemType { get; set; } = "";
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EventRegistration
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }
    public string EventTitle { get; set; } = "";
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

public class TriedDish
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int DishId { get; set; }
    public DateTime TriedAt { get; set; } = DateTime.UtcNow;
}

public class ActivityEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}