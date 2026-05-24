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
    private readonly IWebHostEnvironment _env;

    public AccountController(SqliteDataStore store, IWebHostEnvironment env)
    {
        _store = store;
        _env = env;
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
        var registrations = _store.GetRegistrations(userId);
        ViewData["Registrations"] = registrations;
        ViewData["RegisteredEvents"] = _store.GetEventsByIds(registrations.Select(r => r.EventId).ToList());
        ViewData["Reviews"] = reviews;
        ViewData["ReviewNames"] = _store.ResolveReviewNames(reviews);
        ViewData["Activities"] = _store.GetActivities(userId);
        ViewData["TriedDishes"] = _store.GetTriedDishes(userId);
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

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UploadAvatar(IFormFile avatar)
    {
        if (avatar is null || avatar.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var ext = Path.GetExtension(avatar.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(new { message = "Only JPG, PNG, WEBP, or GIF files are allowed." });

        if (avatar.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "File size must be under 5 MB." });

        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{GetUserId()}{ext}";
        var filePath = Path.Combine(uploadsPath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
            await avatar.CopyToAsync(stream);

        var avatarUrl = $"/uploads/avatars/{fileName}";
        _store.UpdateAvatar(GetUserId(), avatarUrl);
        return Ok(new { url = avatarUrl });
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public IActionResult CheckEmail(string email)
    {
        var user = _store.FindUserByEmail(email.Trim());
        if (user is null)
        {
            TempData["ForgotError"] = "No account found with that email.";
            return RedirectToAction("ForgotPassword");
        }
        TempData["ResetEmail"] = email.Trim();
        return RedirectToAction("ForgotPassword");
    }

    [HttpPost]
    public IActionResult ResetPassword(string email, string newPassword, string confirmPassword)
    {
        if (newPassword != confirmPassword || newPassword.Length < 6)
        {
            TempData["ForgotError"] = "Passwords must match and be at least 6 characters.";
            TempData["ResetEmail"] = email;
            return RedirectToAction("ForgotPassword");
        }

        var user = _store.FindUserByEmail(email.Trim());
        if (user is null)
        {
            TempData["ForgotError"] = "Something went wrong. Please try again.";
            return RedirectToAction("ForgotPassword");
        }

        _store.ChangePassword(user.Id, newPassword);
        TempData["Success"] = "Password reset successfully! You can now sign in.";
        return RedirectToAction("Login");
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