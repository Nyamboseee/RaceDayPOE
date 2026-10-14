using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class RegisterRequest
{
    [Required]
    public string Role { get; set; } = string.Empty; // Organizer or Participant

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    // Only used for Participants
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
}