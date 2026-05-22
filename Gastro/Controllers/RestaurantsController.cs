using GastroCebu.Services;
using Microsoft.AspNetCore.Mvc;

namespace GastroCebu.Controllers;

public class RestaurantsController(SqliteDataStore store) : Controller
{
    public IActionResult Index()
    {
        ViewData["DatabaseRestaurants"] = store.GetRestaurants();
        return View();
    }

    public IActionResult Detail(int id = 1)
    {
        var restaurant = store.GetRestaurantById(id);
        if (restaurant is null) return NotFound();
        var reviews = store.GetReviews("restaurant", id);
        var userReviewIds = new HashSet<int>();
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            userReviewIds = reviews.Where(r => r.UserId == userId).Select(r => r.Id).ToHashSet();
            // Pass current user's userId for edit/delete ownership checks in the view
            ViewData["CurrentUserId"] = userId;
        }
        var reviewerNames = new Dictionary<int, string>();
        foreach (var r in reviews)
        {
            var user = store.FindUserById(r.UserId);
            reviewerNames[r.Id] = user?.FullName ?? "Anonymous";
        }
        ViewData["Restaurant"] = restaurant;
        ViewData["Reviews"] = reviews;
        ViewData["ReviewerNames"] = reviewerNames;
        ViewData["UserReviewIds"] = userReviewIds;
        return View();
    }

    [HttpPost]
    public IActionResult Search(string query = "", string category = "", decimal minRating = 0, string sortBy = "newest")
    {
        var results = store.SearchRestaurants(query, category, minRating, sortBy);
        ViewData["DatabaseRestaurants"] = results;
        ViewData["SearchQuery"] = query;
        ViewData["SelectedCategory"] = category;
        ViewData["SelectedRating"] = minRating;
        ViewData["SelectedSort"] = sortBy;
        return View("Index");
    }

    [HttpGet]
    public IActionResult Search(string query = "")
    {
        var results = store.SearchRestaurants(query, "", 0, "newest");  // ✅
        ViewData["DatabaseRestaurants"] = results;
        ViewData["SearchQuery"] = query;
        ViewData["SelectedCategory"] = "";
        ViewData["SelectedRating"] = 0;
        ViewData["SelectedSort"] = "newest";
        return View("Index");
    }
}