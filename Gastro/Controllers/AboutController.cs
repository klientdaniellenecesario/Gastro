using Microsoft.AspNetCore.Mvc;

namespace GastroCebu.Controllers;

public class AboutController : Controller
{
    public IActionResult Index() => View();
}