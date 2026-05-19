using Microsoft.AspNetCore.Mvc;

namespace GastroCebu.Controllers
{
    public class StoriesController : Controller
    {
        // GET: /Stories
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}