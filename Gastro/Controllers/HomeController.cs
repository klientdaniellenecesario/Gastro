using GastroCebu.Services;
using GastroCebu.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace GastroCebu.Controllers;

public class HomeController(SqliteDataStore store) : Controller
{
    public IActionResult Index()
    {
        // Landing page is for guests only — redirect logged-in users
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Dashboard", "Admin");
            return RedirectToAction("Index", "Restaurants");
        }

        var featuredRestaurants = store.GetRestaurants().Take(6).ToList();
        var trendingDishes = store.GetDishes().Where(d => d.IsTrending).Take(6).ToList();
        var newDishes = store.GetDishes().Where(d => d.IsNewThisMonth).Take(3).ToList();
        var allHomeDishIds = trendingDishes.Concat(newDishes).Select(d => d.Id).Distinct();

        var vm = new HomeViewModel
        {
            FeaturedRestaurants = featuredRestaurants,
            TrendingDishes = trendingDishes,
            NewDishes = newDishes,
            UpcomingEvents = store.GetEvents().Where(e => e.Date >= DateTime.UtcNow).Take(4).ToList(),
            RestaurantCount = store.Count("Restaurants"),
            DishCount = store.Count("Dishes"),
            EventCount = store.Count("Events"),
            UserCount = store.Count("Users")
        };
        ViewData["DishReviewStats"] = store.GetDishReviewStats(allHomeDishIds);
        ViewData["RestaurantReviewStats"] = store.GetRestaurantReviewStats(featuredRestaurants.Select(r => r.Id));
        return View(vm);
    }

    public IActionResult Error(int? statusCode = null)
    {
        if (statusCode == 404)
            Response.StatusCode = 404;
        return View();
    }
}