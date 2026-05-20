using GastroCebu.Services;
using Microsoft.AspNetCore.Mvc;

namespace GastroCebu.Controllers;

public class HomeController(SqliteDataStore store) : Controller
{
    public IActionResult Index()
    {
        ViewData["FeaturedRestaurants"] = store.GetRestaurants().Take(4).ToList();
        ViewData["TrendingDishes"] = store.GetDishes().Where(d => d.IsTrending).Take(4).ToList();
        ViewData["NewDishes"] = store.GetDishes().Where(d => d.IsNewThisMonth).Take(2).ToList();
        ViewData["UpcomingEvents"] = store.GetEvents()
            .Where(e => e.Date >= DateTime.UtcNow)
            .OrderBy(e => e.Date)
            .Take(3)
            .ToList();
        return View();
    }

    public IActionResult Error(int? statusCode = null)
    {
        if (statusCode == 404)
            Response.StatusCode = 404;
        return View();
    }
}