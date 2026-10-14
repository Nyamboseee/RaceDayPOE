
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

    // Both roles can see the categories of an event
    [HttpGet("events/{eventId:int}/categories")]
    [Authorize]
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

    [HttpPost("events/{eventId:int}/categories")]
    [Authorize(Policy = Policies.OrganizerOnly)]
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

    [HttpPut("categories/{id:int}")]
    [Authorize(Policy = Policies.OrganizerOnly)]
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

    [HttpDelete("categories/{id:int}")]
    [Authorize(Policy = Policies.OrganizerOnly)]
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