using System.Security.Claims;
using GastroCebu.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastroCebu.Controllers;

public class AccountController : Controller
{
    private readonly SqliteDataStore _store;

    public AccountController(SqliteDataStore store)
    {
        _store = store;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
    {
        var user = _store.FindUserByEmail(email);
        if (user is null || !SqliteDataStore.VerifyPassword(password, user.PasswordHash))
        {
            TempData["Error"] = "Invalid email or password.";
            return RedirectToAction("Login", new { returnUrl });
        }

        await SignInUser(user.Id, user.FullName, user.Email, user.Role, rememberMe);
        TempData["Success"] = "Welcome back!";
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string fullName, string email, string password, string confirmPassword)
    {
        if (password != confirmPassword || password.Length < 6)
        {
            TempData["Error"] = "Passwords must match and be at least 6 characters.";
            return RedirectToAction("Register");
        }

        if (_store.FindUserByEmail(email) is not null)
        {
            TempData["Error"] = "An account with that email already exists.";
            return RedirectToAction("Register");
        }

        var user = _store.CreateUser(fullName.Trim(), email.Trim(), password);
        await SignInUser(user.Id, user.FullName, user.Email, user.Role, false);
        TempData["Success"] = "Account created.";
        return RedirectToAction("Profile");
    }

    [Authorize]
    [HttpGet]
    public IActionResult Profile()
    {
        var userId = GetUserId();
        var reviews = _store.GetUserReviews(userId);
        ViewData["ProfileUser"] = _store.FindUserById(userId);
        ViewData["RestaurantBookmarks"] = _store.GetBookmarks(userId, "restaurant");
        ViewData["DishBookmarks"] = _store.GetBookmarks(userId, "dish");
        ViewData["Registrations"] = _store.GetRegistrations(userId);
        ViewData["Reviews"] = reviews;
        ViewData["ReviewNames"] = _store.ResolveReviewNames(reviews);
        ViewData["Activities"] = _store.GetActivities(userId);
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    public IActionResult UpdateProfile(string fullName, string location)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(location))
        {
            TempData["Error"] = "Profile fields are required.";
            return RedirectToAction("Profile");
        }

        _store.UpdateUser(GetUserId(), fullName.Trim(), location.Trim());
        TempData["Success"] = "Profile updated.";
        return RedirectToAction("Profile");
    }

    [Authorize]
    [HttpPost]
    public IActionResult ChangePassword(string currentPassword, string newPassword)
    {
        var user = _store.FindUserById(GetUserId());
        if (user is null || !SqliteDataStore.VerifyPassword(currentPassword, user.PasswordHash) || newPassword.Length < 6)
        {
            TempData["Error"] = "Password change failed.";
            return RedirectToAction("Profile");
        }

        _store.ChangePassword(user.Id, newPassword);
        TempData["Success"] = "Password changed.";
        return RedirectToAction("Profile");
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task SignInUser(int id, string fullName, string email, string role, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id.ToString()),
            new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = rememberMe });
    }
}