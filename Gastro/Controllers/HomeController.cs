using GastroCebu.Services;
using GastroCebu.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace GastroCebu.Controllers;

public class HomeController(SqliteDataStore store) : Controller
{
    public IActionResult Index()
    {
        var vm = new HomeViewModel
        {
            FeaturedRestaurants = store.GetRestaurants().Take(6).ToList(),
            TrendingDishes = store.GetDishes().Where(d => d.IsTrending).Take(6).ToList(),
            NewDishes = store.GetDishes().Where(d => d.IsNewThisMonth).Take(3).ToList(),
            UpcomingEvents = store.GetEvents().Where(e => e.Date >= DateTime.UtcNow).Take(4).ToList(),
            RestaurantCount = store.Count("Restaurants"),
            DishCount = store.Count("Dishes"),
            EventCount = store.Count("Events"),
            UserCount = store.Count("Users")
        };
        return View(vm);
    }

    public IActionResult Error(int? statusCode = null)
    {
        if (statusCode == 404)
            Response.StatusCode = 404;
        return View();
    }
}