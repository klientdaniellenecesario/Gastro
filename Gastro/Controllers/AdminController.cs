using GastroCebu.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastroCebu.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(SqliteDataStore store, IWebHostEnvironment env) : Controller
{
    public IActionResult Dashboard()
    {
        ViewData["RestaurantCount"] = store.Count("Restaurants");
        ViewData["DishCount"] = store.Count("Dishes");
        ViewData["EventCount"] = store.Count("Events");
        ViewData["UserCount"] = store.Count("Users");
        ViewData["ReviewCount"] = store.Count("Reviews");
        ViewData["Restaurants"] = store.GetRestaurants();
        ViewData["Dishes"] = store.GetDishes();
        ViewData["Events"] = store.GetEvents();
        ViewData["Users"] = store.GetUsers();
        var reviews = store.GetAllReviews();
        ViewData["Reviews"] = reviews;
        ViewData["ReviewUserNames"] = store.GetUserNames(reviews);
        ViewData["ReviewTargetNames"] = store.ResolveReviewNames(reviews);
        ViewData["RecentActivities"] = store.GetRecentActivities(10);
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddItem(string type, string name, IFormFile? photo, string? imageUrl, string description,
        string? address, string? category,
        decimal price = 0, string? tags = null, bool isNew = false, bool isTrending = false,
        string? location = null, int availableSlots = 20, string? eventDate = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Name is required.";
            return RedirectToAction("Dashboard");
        }

        var resolvedUrl = await SavePhotoAsync(photo, imageUrl) ?? "";
        description ??= "";

        if (type == "restaurant")
            store.AddRestaurant(name, resolvedUrl, description, address ?? "Cebu", category ?? "Restaurant");
        else if (type == "dish")
        {
            var rid = Request.Form.TryGetValue("restaurantId", out var rv) && int.TryParse(rv, out var rId) ? (int?)rId : null;
            store.AddDish(name, resolvedUrl, description, price, tags ?? "", isNew, isTrending, rid);
        }
        else if (type == "event")
        {
            var date = DateTime.TryParse(eventDate, out var d) ? d.ToUniversalTime() : DateTime.UtcNow.AddDays(30);
            store.AddEvent(name, resolvedUrl, description, date, location ?? "Cebu City", availableSlots);
        }

        TempData["Success"] = "Item saved.";
        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    public async Task<IActionResult> EditItem(string type, int id, string name, IFormFile? photo, string? imageUrl, string description,
        string? address, string? category,
        decimal price = 0, string? tags = null, bool isNew = false, bool isTrending = false,
        string? location = null, int availableSlots = 0, string? eventDate = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Name is required.";
            return RedirectToAction("Dashboard");
        }

        var resolvedUrl = await SavePhotoAsync(photo, imageUrl);

        if (type == "restaurant")
            store.UpdateRestaurant(id, name, address ?? "Cebu", category ?? "Restaurant", resolvedUrl ?? "", description ?? "");
        else if (type == "dish")
        {
            var rid = Request.Form.TryGetValue("restaurantId", out var rv) && int.TryParse(rv, out var rId) ? (int?)rId : null;
            store.UpdateDish(id, name, price, resolvedUrl ?? "", description ?? "", tags ?? "", isNew, isTrending, rid);
        }
        else if (type == "event")
        {
            var date = DateTime.TryParse(eventDate, out var d) ? d.ToUniversalTime() : DateTime.UtcNow.AddDays(30);
            store.UpdateEvent(id, name, date, location ?? "Cebu City", availableSlots, resolvedUrl ?? "", description ?? "");
        }

        TempData["Success"] = "Item updated.";
        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    public IActionResult DeleteItem(string type, int id)
    {
        store.DeleteFromAdmin(type, id);
        TempData["Success"] = "Item deleted.";
        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    public IActionResult DeleteUser(int id)
    {
        var target = store.FindUserById(id);
        if (target == null || target.Email == "admin@tastecebu.test")
        {
            TempData["Error"] = "The system admin account cannot be deleted.";
            return RedirectToAction("Dashboard");
        }
        store.DeleteFromAdmin("user", id);
        TempData["Success"] = $"{target.FullName} has been deleted.";
        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    public IActionResult EditUser(int id, string fullName, string email)
    {
        var target = store.FindUserById(id);
        if (target == null || target.Email == "admin@tastecebu.test")
        {
            TempData["Error"] = "The system admin account cannot be edited.";
            return RedirectToAction("Dashboard");
        }
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Name and email are required.";
            return RedirectToAction("Dashboard");
        }
        store.AdminUpdateUser(id, fullName.Trim(), email.Trim());
        TempData["Success"] = "User updated.";
        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    public IActionResult SetAdmin(int id, bool isAdmin)
    {
        // Block: never touch the seeded system admin account
        var target = store.FindUserById(id);
        if (target == null || target.Email == "admin@tastecebu.test")
        {
            TempData["Error"] = "The system admin account cannot be modified.";
            return RedirectToAction("Dashboard");
        }
        // Block: cannot promote regular users to admin
        if (isAdmin)
        {
            TempData["Error"] = "Users cannot be promoted to Admin.";
            return RedirectToAction("Dashboard");
        }
        store.SetAdminRole(id, false);
        TempData["Success"] = "Admin role removed.";
        return RedirectToAction("Dashboard");
    }

    private static readonly string[] AllowedPhotoExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxPhotoBytes = 5 * 1024 * 1024; // 5 MB

    private async Task<string?> SavePhotoAsync(IFormFile? photo, string? fallbackUrl)
    {
        if (photo is { Length: > 0 })
        {
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!AllowedPhotoExtensions.Contains(ext) || photo.Length > MaxPhotoBytes)
                return string.IsNullOrWhiteSpace(fallbackUrl) ? null : fallbackUrl;

            var uploads = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploads, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await photo.CopyToAsync(stream);
            return $"/uploads/{fileName}";
        }
        return string.IsNullOrWhiteSpace(fallbackUrl) ? null : fallbackUrl;
    }
}