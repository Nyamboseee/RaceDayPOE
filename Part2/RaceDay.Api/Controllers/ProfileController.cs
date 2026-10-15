using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ProfileController(RaceDayDbContext db)
    {
        _db = db;
    }

    // The id and role come from the login cookie, never from the request
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsOrganizer => User.IsInRole(Roles.Organizer);

    /// <summary>Returns the profile of the logged-in user.</summary>
    /// <remarks>Organizers and Participants hold different details, so the fields returned depend on the role.</remarks>
    /// <response code="200">The user's own profile.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">The account no longer exists.</response>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile()
    {
        if (IsOrganizer)
        {
            var organizer = await _db.Organizers.FindAsync(CurrentUserId);
            if (organizer == null) return NotFound();

            return Ok(new
            {
                id = organizer.OrganizerId,
                role = Roles.Organizer,
                fullName = organizer.FullName,
                email = organizer.Email,
                contactNumber = organizer.ContactNumber
            });
        }

        var participant = await _db.Participants.FindAsync(CurrentUserId);
        if (participant == null) return NotFound();

        return Ok(new
        {
            id = participant.ParticipantId,
            role = Roles.Participant,
            firstName = participant.FirstName,
            lastName = participant.LastName,
            email = participant.Email,
            phone = participant.Phone,
            gender = participant.Gender,
            dateOfBirth = participant.DateOfBirth
        });
    }

    /// <summary>Updates the logged-in user's own profile.</summary>
    /// <remarks>Organizers send fullName and phoneNumber. Participants send firstName, lastName, phoneNumber, gender and dateOfBirth. Email cannot be changed.</remarks>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">A required name field is missing.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">The account no longer exists.</response>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileRequest request)
    {
        if (IsOrganizer)
        {
            var organizer = await _db.Organizers.FindAsync(CurrentUserId);
            if (organizer == null) return NotFound();

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest("FullName is required for an Organizer.");
            }

            organizer.FullName = request.FullName.Trim();
            organizer.ContactNumber = request.PhoneNumber;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                id = organizer.OrganizerId,
                role = Roles.Organizer,
                fullName = organizer.FullName,
                contactNumber = organizer.ContactNumber
            });
        }

        var participant = await _db.Participants.FindAsync(CurrentUserId);
        if (participant == null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest("FirstName and LastName are required for a Participant.");
        }

        participant.FirstName = request.FirstName.Trim();
        participant.LastName = request.LastName.Trim();
        participant.Phone = request.PhoneNumber;
        participant.Gender = request.Gender;
        participant.DateOfBirth = request.DateOfBirth;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = participant.ParticipantId,
            role = Roles.Participant,
            firstName = participant.FirstName,
            lastName = participant.LastName,
            phone = participant.Phone,
            gender = participant.Gender,
            dateOfBirth = participant.DateOfBirth
        });
    }
}