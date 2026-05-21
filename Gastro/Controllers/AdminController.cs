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
        ViewData["Reviews"] = store.GetAllReviews();
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
            store.AddDish(name, resolvedUrl, description, price, tags ?? "", isNew, isTrending);
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
            store.UpdateDish(id, name, price, resolvedUrl ?? "", description ?? "", tags ?? "", isNew, isTrending);
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
    public IActionResult SetAdmin(int id, bool isAdmin)
    {
        store.SetAdminRole(id, isAdmin);
        TempData["Success"] = "User role updated.";
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