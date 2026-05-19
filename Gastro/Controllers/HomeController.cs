// File: Controllers/HomeController.cs (minimal for routing context)
using Microsoft.AspNetCore.Mvc;

namespace GastroCebu.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }

    public class RestaurantsController : Controller
    {
        public IActionResult Index() => View();
    }

    public class DishesController : Controller
    {
        public IActionResult Index() => View();
    }

    public class EventsController : Controller
    {
        public IActionResult Index() => View();
    }

    public class AboutController : Controller
    {
        public IActionResult Index() => View();
    }
}