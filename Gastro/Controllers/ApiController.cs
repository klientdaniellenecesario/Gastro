using System.Security.Claims;
using GastroCebu.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastroCebu.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class ApiController(SqliteDataStore store) : ControllerBase
{
    [HttpPost("bookmarks")]
    public IActionResult ToggleBookmark([FromBody] BookmarkRequest request)
    {
        if (request.ItemId <= 0 || string.IsNullOrWhiteSpace(request.ItemName) || string.IsNullOrWhiteSpace(request.ItemType))
            return BadRequest(new { message = "Invalid bookmark." });

        store.ToggleBookmark(UserId, request.ItemType, request.ItemId, request.ItemName);
        return Ok(new { message = $"{request.ItemName} bookmark updated." });
    }

    [HttpPost("reviews")]
    public IActionResult AddReview([FromBody] ReviewRequest request)
    {
        if (request.Rating is < 1 or > 5 || string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { message = "Rating and review text are required." });

        store.AddReview(UserId, request.TargetType, request.TargetId, request.Rating, request.Text.Trim());
        return Ok(new { message = "Review submitted." });
    }

    [HttpPut("reviews/{id:int}")]
    public IActionResult UpdateReview(int id, [FromBody] EditReviewRequest request)
    {
        if (request.Rating is < 1 or > 5 || string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { message = "Rating (1–5) and review text are required." });

        var updated = store.UpdateReview(UserId, id, request.Rating, request.Text.Trim());
        if (!updated) return NotFound(new { message = "Review not found or not yours." });
        return Ok(new { message = "Review updated." });
    }

    [HttpDelete("reviews/{id:int}")]
    public IActionResult DeleteReview(int id)
    {
        store.DeleteReview(UserId, id, User.IsInRole("Admin"));
        return Ok(new { message = "Review deleted." });
    }

    [HttpPost("events/{eventId:int}/register")]
    public IActionResult RegisterEvent(int eventId)
    {
        try
        {
            store.RegisterForEvent(UserId, eventId);
            return Ok(new { message = "Registered for event." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("events/{eventId:int}/register")]
    public IActionResult CancelEvent(int eventId)
    {
        store.CancelEventRegistration(UserId, eventId);
        return Ok(new { message = "Event registration cancelled." });
    }

    [HttpGet("events/{eventId:int}/registrants")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetEventRegistrants(int eventId)
    {
        var registrants = store.GetEventRegistrants(eventId)
            .Select(u => new { u.FullName, u.Email })
            .ToList();
        return Ok(registrants);
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public record BookmarkRequest(string ItemType, int ItemId, string ItemName);
public record ReviewRequest(string TargetType, int TargetId, int Rating, string Text);
public record EditReviewRequest(int Rating, string Text);