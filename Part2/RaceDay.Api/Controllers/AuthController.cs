using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();

        int id;
        string role;
        string name;
        string hash;

        var organizer = await _db.Organizers.FirstOrDefaultAsync(o => o.Email == email);
        if (organizer != null)
        {
            id = organizer.OrganizerId;
            role = "Organizer";
            name = organizer.FullName;
            hash = organizer.PasswordHash;
        }
        else
        {
            var participant = await _db.Participants.FirstOrDefaultAsync(p => p.Email == email);
            if (participant == null)
            {
                return Unauthorized("Invalid email or password.");
            }
            id = participant.ParticipantId;
            role = "Participant";
            name = $"{participant.FirstName} {participant.LastName}";
            hash = participant.PasswordHash;
        }

        if (!_passwords.Verify(hash, request.Password))
        {
            return Unauthorized("Invalid email or password.");
        }

        // The claims are what the cookie remembers about this user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return Ok(new { id, name, email, role });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            name = User.FindFirstValue(ClaimTypes.Name),
            email = User.FindFirstValue(ClaimTypes.Email),
            role = User.FindFirstValue(ClaimTypes.Role)
        });
    }
}