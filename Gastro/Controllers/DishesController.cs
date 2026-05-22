using GastroCebu.Services;
using Microsoft.AspNetCore.Mvc;

namespace GastroCebu.Controllers;

public class DishesController(SqliteDataStore store) : Controller
{
    public IActionResult Index()
    {
        ViewData["DatabaseDishes"] = store.GetDishes();
        return View();
    }

    public IActionResult Detail(int id = 1)
    {
        var dish = store.GetDishById(id);
        if (dish is null) return NotFound();
        var reviews = store.GetReviews("dish", id);
        var userReviewIds = new HashSet<int>();
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            userReviewIds = reviews.Where(r => r.UserId == userId).Select(r => r.Id).ToHashSet();
            ViewData["CurrentUserId"] = userId;
        }
        var reviewerNames = new Dictionary<int, string>();
        foreach (var r in reviews)
        {
            var user = store.FindUserById(r.UserId);
            reviewerNames[r.Id] = user?.FullName ?? "Anonymous";
        }
        ViewData["Dish"] = dish;
        ViewData["Reviews"] = reviews;
        ViewData["ReviewerNames"] = reviewerNames;
        ViewData["UserReviewIds"] = userReviewIds;
        return View();
    }

    [HttpPost]
    public IActionResult Search(string query = "", string tags = "", string sortBy = "newest")
    {
        var results = store.SearchDishes(query, tags, sortBy);
        ViewData["DatabaseDishes"] = results;
        ViewData["SearchQuery"] = query;
        ViewData["SelectedTags"] = tags;
        ViewData["SelectedSort"] = sortBy;
        return View("Index");
    }


    [HttpGet]
    public IActionResult Search(string query = "")
    {
        var results = store.SearchDishes(query, "", "newest");  // ✅ calls store directly
        ViewData["DatabaseDishes"] = results;
        ViewData["SearchQuery"] = query;
        ViewData["SelectedTags"] = "";
        ViewData["SelectedSort"] = "newest";
        return View("Index");
    }
}
