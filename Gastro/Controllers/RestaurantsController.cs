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
}