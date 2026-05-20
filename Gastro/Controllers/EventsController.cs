using GastroCebu.Services;
using Microsoft.AspNetCore.Mvc;

namespace GastroCebu.Controllers;

public class EventsController(SqliteDataStore store) : Controller
{
    public IActionResult Index()
    {
        ViewData["DatabaseEvents"] = store.GetEvents();
        return View();
    }

    public IActionResult Detail(int id = 1)
    {
        ViewData["ItemId"] = id;
        return View();
    }
}