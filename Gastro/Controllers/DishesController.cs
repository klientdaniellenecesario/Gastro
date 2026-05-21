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
        ViewData["ItemId"] = id;
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
