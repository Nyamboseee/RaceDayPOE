using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordService _passwords;

    public AuthController(RaceDayDbContext db, IPasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var role = request.Role.Trim().ToLower();
        if (role != "organizer" && role != "organiser" && role != "participant")
        {
            return BadRequest("Role must be Organizer or Participant.");
        }

        var email = request.Email.Trim().ToLower();

        // An email can only belong to one account across both tables
        var taken = await _db.Organizers.AnyAsync(o => o.Email == email)
                 || await _db.Participants.AnyAsync(p => p.Email == email);
        if (taken)
        {
            return Conflict("An account with this email already exists.");
        }

        var hash = _passwords.Hash(request.Password);

        if (role == "participant")
        {
            var participant = new Participant
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = email,
                PasswordHash = hash,
                Phone = request.PhoneNumber,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth
            };
            _db.Participants.Add(participant);
            await _db.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created,
                new { id = participant.ParticipantId, role = "Participant", email });
        }

        var organizer = new Organizer
        {
            FullName = $"{request.FirstName} {request.LastName}",
            Role = "Organizer",
            ContactNumber = request.PhoneNumber,
            Email = email,
            PasswordHash = hash
        };
        _db.Organizers.Add(organizer);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created,
            new { id = organizer.OrganizerId, role = "Organizer", email });
    }
}