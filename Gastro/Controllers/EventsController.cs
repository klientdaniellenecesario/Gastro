using System.Security.Claims;
using GastroCebu.Services;
using Microsoft.AspNetCore.Mvc;

namespace GastroCebu.Controllers;

public class EventsController(SqliteDataStore store) : Controller
{
    public IActionResult Index()
    {
        ViewData["DatabaseEvents"] = store.GetEvents();
        if (User.Identity?.IsAuthenticated == true)
        {
            var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            ViewData["RegisteredEventIds"] = store.GetRegisteredEventIds(uid);
        }
        return View();
    }

    public IActionResult Detail(int id = 1)
    {
        var events = store.GetEvents();
        var ev = events.FirstOrDefault(e => e.Id == id) ?? events.FirstOrDefault();
        if (ev == null) return NotFound();
        var registrants = store.GetEventRegistrants(ev.Id);
        ViewData["Event"] = ev;
        ViewData["Registrants"] = registrants;
        if (User.Identity?.IsAuthenticated == true)
        {
            var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            ViewData["IsRegistered"] = store.IsUserRegistered(uid, ev.Id);
        }
        return View();
    }
}