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

    /// <summary>Registers a new Organizer or Participant account.</summary>
    /// <remarks>The role field decides which account type is created. The password is hashed before it is stored.</remarks>
    /// <response code="201">Account created. Returns the new id, role and email.</response>
    /// <response code="400">Validation failed, for example an unknown role or a password shorter than 8 characters.</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

    /// <summary>Logs a user in and starts a cookie session.</summary>
    /// <remarks>The session cookie carries the user's id and role for every later request.</remarks>
    /// <response code="200">Logged in. Returns the id, name, email and role.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>Ends the current session.</summary>
    /// <response code="204">Logged out.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>Returns who the current session belongs to.</summary>
    /// <response code="200">The logged-in user's id, name, email and role.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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