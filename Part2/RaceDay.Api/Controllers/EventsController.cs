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
[Route("api/events")]
public class EventsController : ControllerBase
{
    private static readonly string[] ValidTypes = { "Run", "Walk", "Cycle" };

    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Both roles can view events, so this only needs a login
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _db.Events
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                id = e.EventId,
                name = e.EventName,
                description = e.Description,
                date = e.EventDate,
                location = e.Location,
                distance = e.Distance,
                eventType = e.EventType,
                organizerId = e.OrganizerId
            })
            .ToListAsync();

        return Ok(events);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetEvent(int id)
    {
        var e = await _db.Events.FindAsync(id);
        if (e == null) return NotFound();

        return Ok(new
        {
            id = e.EventId,
            name = e.EventName,
            description = e.Description,
            date = e.EventDate,
            location = e.Location,
            distance = e.Distance,
            eventType = e.EventType,
            organizerId = e.OrganizerId
        });
    }

    [HttpPost]
    [Authorize(Policy = Policies.OrganizerOnly)]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        var type = ValidTypes.FirstOrDefault(t =>
            t.Equals(request.EventType.Trim(), StringComparison.OrdinalIgnoreCase));
        if (type == null)
        {
            return BadRequest("EventType must be Run, Walk or Cycle.");
        }

        var newEvent = new Event
        {
            OrganizerId = CurrentUserId,
            EventName = request.EventName.Trim(),
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            Distance = request.Distance,
            EventType = type
        };

        _db.Events.Add(newEvent);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = newEvent.EventId },
            new { id = newEvent.EventId, name = newEvent.EventName });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.OrganizerOnly)]
    public async Task<IActionResult> UpdateEvent(int id, EventRequest request)
    {
        var existing = await _db.Events.FindAsync(id);
        if (existing == null) return NotFound();

        // An Organizer can only change their own events
        if (existing.OrganizerId != CurrentUserId) return Forbid();

        var type = ValidTypes.FirstOrDefault(t =>
            t.Equals(request.EventType.Trim(), StringComparison.OrdinalIgnoreCase));
        if (type == null)
        {
            return BadRequest("EventType must be Run, Walk or Cycle.");
        }

        existing.EventName = request.EventName.Trim();
        existing.Description = request.Description;
        existing.EventDate = request.EventDate;
        existing.Location = request.Location.Trim();
        existing.Distance = request.Distance;
        existing.EventType = type;
        await _db.SaveChangesAsync();

        return Ok(new { id = existing.EventId, name = existing.EventName });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.OrganizerOnly)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var existing = await _db.Events.FindAsync(id);
        if (existing == null) return NotFound();

        if (existing.OrganizerId != CurrentUserId) return Forbid();

        // Events with categories or enrolments cannot be removed
        var inUse = await _db.Categories.AnyAsync(c => c.EventId == id)
                 || await _db.Enrollments.AnyAsync(en => en.EventId == id);
        if (inUse)
        {
            return Conflict("This event has categories or enrolments and cannot be deleted.");
        }

        _db.Events.Remove(existing);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}