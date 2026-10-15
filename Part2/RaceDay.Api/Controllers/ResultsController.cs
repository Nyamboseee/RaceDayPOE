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

    /// <summary>Captures a finish time and position for an enrolment.</summary>
    /// <remarks>Organizer only, and only for events they created. Status is Finished, DNF or DNS. A Finished result needs a finishTime (hh:mm:ss) and a position.</remarks>
    /// <response code="201">Result captured.</response>
    /// <response code="400">Invalid status, or a Finished result is missing its time or position.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organizer, or the event belongs to another Organizer.</response>
    /// <response code="404">No enrolment with that id.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpPost("enrollments/{enrollmentId:int}/results")]
    [Authorize(Policy = Policies.OrganizerOnly)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

    /// <summary>Corrects a result that was captured earlier.</summary>
    /// <remarks>Organizer only, and only for events they created.</remarks>
    /// <response code="200">Result updated.</response>
    /// <response code="400">Invalid status, or a Finished result is missing its time or position.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organizer, or the event belongs to another Organizer.</response>
    /// <response code="404">No result with that id.</response>
    [HttpPut("results/{id:int}")]
    [Authorize(Policy = Policies.OrganizerOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>Lists the logged-in Participant's own results.</summary>
    /// <remarks>Participant only. A participant only ever sees their own results.</remarks>
    /// <response code="200">The participant's results, most recent event first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organizer.</response>
    [HttpGet("results/me")]
    [Authorize(Policy = Policies.ParticipantOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

    /// <summary>Lists the results of an event.</summary>
    /// <remarks>Both roles can view an event's results. Finished runners come first, ordered by position.</remarks>
    /// <response code="200">The results for the event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("events/{eventId:int}/results")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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