using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lists the categories of an event.</summary>
    /// <remarks>Both Organizers and Participants can view categories.</remarks>
    /// <response code="200">The categories, shortest distance first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return NotFound("Event not found.");
        }

        var categories = await _db.Categories
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.Distance)
            .Select(c => new
            {
                id = c.CategoryId,
                eventId = c.EventId,
                name = c.CategoryName,
                distance = c.Distance,
                minAge = c.MinAge,
                maxAge = c.MaxAge
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Adds a category to an event.</summary>
    /// <remarks>Organizer only, and only for events they created. A category has a name, a distance and an optional age range.</remarks>
    /// <response code="201">Category created.</response>
    /// <response code="400">MinAge is greater than MaxAge.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organizer, or the event belongs to another Organizer.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPost("events/{eventId:int}/categories")]
    [Authorize(Policy = Policies.OrganizerOnly)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound("Event not found.");

        // An Organizer can only add categories to their own events
        if (ev.OrganizerId != CurrentUserId) return Forbid();

        if (request.MinAge > request.MaxAge)
        {
            return BadRequest("MinAge cannot be greater than MaxAge.");
        }

        var category = new Category
        {
            EventId = eventId,
            CategoryName = request.CategoryName.Trim(),
            Distance = request.Distance,
            MinAge = request.MinAge,
            MaxAge = request.MaxAge
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created,
            new { id = category.CategoryId, eventId, name = category.CategoryName });
    }

    /// <summary>Updates a category.</summary>
    /// <remarks>Organizer only, and only for categories under their own events.</remarks>
    /// <response code="200">Category updated.</response>
    /// <response code="400">MinAge is greater than MaxAge.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organizer, or the event belongs to another Organizer.</response>
    /// <response code="404">No category with that id.</response>
    [HttpPut("categories/{id:int}")]
    [Authorize(Policy = Policies.OrganizerOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound();

        if (category.Event!.OrganizerId != CurrentUserId) return Forbid();

        if (request.MinAge > request.MaxAge)
        {
            return BadRequest("MinAge cannot be greater than MaxAge.");
        }

        category.CategoryName = request.CategoryName.Trim();
        category.Distance = request.Distance;
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        await _db.SaveChangesAsync();

        return Ok(new { id = category.CategoryId, name = category.CategoryName });
    }

    /// <summary>Deletes a category.</summary>
    /// <remarks>Organizer only. A category that already has enrolments cannot be deleted.</remarks>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organizer, or the event belongs to another Organizer.</response>
    /// <response code="404">No category with that id.</response>
    /// <response code="409">The category has enrolments.</response>
    [HttpDelete("categories/{id:int}")]
    [Authorize(Policy = Policies.OrganizerOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound();

        if (category.Event!.OrganizerId != CurrentUserId) return Forbid();

        // A category with enrolments cannot be removed
        if (await _db.Enrollments.AnyAsync(e => e.CategoryId == id))
        {
            return Conflict("This category has enrolments and cannot be deleted.");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}