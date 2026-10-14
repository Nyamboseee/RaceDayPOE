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
public class ResultsController : ControllerBase
{
    private static readonly string[] ValidStatuses = { "Finished", "DNF", "DNS" };

    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Returns an error message, or null when the request makes sense
    private static string? Validate(ResultRequest request, out string status)
    {
        status = ValidStatuses.FirstOrDefault(s =>
            s.Equals(request.Status.Trim(), StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

        if (status == string.Empty)
            return "Status must be Finished, DNF or DNS.";

        if (status == "Finished" && (request.FinishTime == null || request.Position == null))
            return "A finished result needs a FinishTime and a Position.";

        return null;
    }

    [HttpPost("enrollments/{enrollmentId:int}/results")]
    [Authorize(Policy = Policies.OrganizerOnly)]
    public async Task<IActionResult> CaptureResult(int enrollmentId, ResultRequest request)
    {
        var enrollment = await _db.Enrollments.Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);
        if (enrollment == null) return NotFound("Enrolment not found.");

        // Only the Organizer of that event can capture its results
        if (enrollment.Event!.OrganizerId != CurrentUserId) return Forbid();

        var error = Validate(request, out var status);
        if (error != null) return BadRequest(error);

        if (await _db.Results.AnyAsync(r => r.EnrollmentId == enrollmentId))
        {
            return Conflict("A result has already been captured. Use PUT to correct it.");
        }

        var result = new Result
        {
            EnrollmentId = enrollmentId,
            FinishTime = status == "Finished" ? request.FinishTime : null,
            Position = status == "Finished" ? request.Position : null,
            Status = status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            id = result.ResultId,
            enrollmentId,
            finishTime = result.FinishTime,
            position = result.Position,
            status = result.Status
        });
    }

    [HttpPut("results/{id:int}")]
    [Authorize(Policy = Policies.OrganizerOnly)]
    public async Task<IActionResult> UpdateResult(int id, ResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Enrollment).ThenInclude(e => e!.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null) return NotFound();

        if (result.Enrollment!.Event!.OrganizerId != CurrentUserId) return Forbid();

        var error = Validate(request, out var status);
        if (error != null) return BadRequest(error);

        result.FinishTime = status == "Finished" ? request.FinishTime : null;
        result.Position = status == "Finished" ? request.Position : null;
        result.Status = status;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = result.ResultId,
            finishTime = result.FinishTime,
            position = result.Position,
            status = result.Status
        });
    }

    // A participant's own results
    [HttpGet("results/me")]
    [Authorize(Policy = Policies.ParticipantOnly)]
    public async Task<IActionResult> GetMyResults()
    {
        var mine = await _db.Results
            .Where(r => r.Enrollment!.ParticipantId == CurrentUserId)
            .OrderByDescending(r => r.Enrollment!.Event!.EventDate)
            .Select(r => new
            {
                id = r.ResultId,
                eventName = r.Enrollment!.Event!.EventName,
                eventDate = r.Enrollment.Event.EventDate,
                categoryName = r.Enrollment.Category!.CategoryName,
                bibNumber = r.Enrollment.BibNumber,
                finishTime = r.FinishTime,
                position = r.Position,
                status = r.Status
            })
            .ToListAsync();

        return Ok(mine);
    }

    // Everyone logged in can see an event's results
    [HttpGet("events/{eventId:int}/results")]
    [Authorize]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return NotFound("Event not found.");
        }

        var results = await _db.Results
            .Where(r => r.Enrollment!.EventId == eventId)
            .OrderBy(r => r.Position == null)
            .ThenBy(r => r.Position)
            .Select(r => new
            {
                position = r.Position,
                participantName = r.Enrollment!.Participant!.FirstName + " " + r.Enrollment.Participant.LastName,
                bibNumber = r.Enrollment.BibNumber,
                categoryName = r.Enrollment.Category!.CategoryName,
                finishTime = r.FinishTime,
                status = r.Status
            })
            .ToListAsync();

        return Ok(results);
    }
}