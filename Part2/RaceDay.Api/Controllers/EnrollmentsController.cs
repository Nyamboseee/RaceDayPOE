using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class EnrollmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrollmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // A participant enters an event by choosing one of its categories
    [HttpPost("categories/{categoryId:int}/enrollments")]
    [Authorize(Policy = Policies.ParticipantOnly)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var category = await _db.Categories.FindAsync(categoryId);
        if (category == null) return NotFound("Category not found.");

        var alreadyEnrolled = await _db.Enrollments
            .AnyAsync(e => e.ParticipantId == CurrentUserId && e.CategoryId == categoryId);
        if (alreadyEnrolled)
        {
            return Conflict("You are already enrolled in this category.");
        }

        var enrollment = new Enrollment
        {
            ParticipantId = CurrentUserId,
            EventId = category.EventId,   // the event comes from the category
            CategoryId = categoryId,
            BibNumber = await NextBibNumber(category.EventId)
        };

        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            id = enrollment.EnrollmentId,
            eventId = enrollment.EventId,
            categoryId = enrollment.CategoryId,
            bibNumber = enrollment.BibNumber
        });
    }

    // A participant's own enrolments
    [HttpGet("enrollments/me")]
    [Authorize(Policy = Policies.ParticipantOnly)]
    public async Task<IActionResult> GetMyEnrollments()
    {
        var mine = await _db.Enrollments
            .Where(e => e.ParticipantId == CurrentUserId)
            .OrderByDescending(e => e.EnrollmentDate)
            .Select(e => new
            {
                id = e.EnrollmentId,
                eventId = e.EventId,
                eventName = e.Event!.EventName,
                eventDate = e.Event.EventDate,
                categoryId = e.CategoryId,
                categoryName = e.Category!.CategoryName,
                bibNumber = e.BibNumber,
                enrolledOn = e.EnrollmentDate
            })
            .ToListAsync();

        return Ok(mine);
    }

    // An organizer sees everyone enrolled in one of their own events
    [HttpGet("events/{eventId:int}/enrollments")]
    [Authorize(Policy = Policies.OrganizerOnly)]
    public async Task<IActionResult> GetEventEnrollments(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound("Event not found.");

        if (ev.OrganizerId != CurrentUserId) return Forbid();

        var enrollments = await _db.Enrollments
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.BibNumber)
            .Select(e => new
            {
                id = e.EnrollmentId,
                participantId = e.ParticipantId,
                participantName = e.Participant!.FirstName + " " + e.Participant.LastName,
                categoryName = e.Category!.CategoryName,
                bibNumber = e.BibNumber,
                enrolledOn = e.EnrollmentDate
            })
            .ToListAsync();

        return Ok(enrollments);
    }

    // A participant can cancel their own enrolment
    [HttpDelete("enrollments/{id:int}")]
    [Authorize(Policy = Policies.ParticipantOnly)]
    public async Task<IActionResult> CancelEnrollment(int id)
    {
        var enrollment = await _db.Enrollments.FindAsync(id);
        if (enrollment == null) return NotFound();

        if (enrollment.ParticipantId != CurrentUserId) return Forbid();

        // Once a result has been captured the enrolment is part of the record
        if (await _db.Results.AnyAsync(r => r.EnrollmentId == id))
        {
            return Conflict("This enrolment already has a result and cannot be cancelled.");
        }

        _db.Enrollments.Remove(enrollment);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // Bib numbers look like RD-7-0001 and are unique within the event
    private async Task<string> NextBibNumber(int eventId)
    {
        var number = await _db.Enrollments.CountAsync(e => e.EventId == eventId) + 1;
        string bib;
        do
        {
            bib = $"RD-{eventId}-{number:D4}";
            number++;
        } while (await _db.Enrollments.AnyAsync(e => e.BibNumber == bib));

        return bib;
    }
}