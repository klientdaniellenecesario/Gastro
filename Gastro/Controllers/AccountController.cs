using Microsoft.AspNetCore.Mvc;

namespace GastroCebu.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(string email, string password, bool rememberMe)
        {
            // TODO: Add actual authentication logic here
            // For now, just redirect to profile
            return RedirectToAction("Profile", "Account");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public IActionResult Register(string fullName, string email, string password, string confirmPassword)
        {
            // TODO: Add actual registration logic here
            // For now, just redirect to profile
            return RedirectToAction("Profile", "Account");
        }

        // GET: /Account/Profile
        [HttpGet]
        public IActionResult Profile()
        {
            return View();
        }

        // GET: /Account/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            // TODO: Add actual logout logic here
            return RedirectToAction("Index", "Home");
        }
    }
}