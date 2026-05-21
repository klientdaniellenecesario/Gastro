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
        ViewData["ItemId"] = id;
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