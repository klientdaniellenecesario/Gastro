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
}