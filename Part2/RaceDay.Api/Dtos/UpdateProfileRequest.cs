using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class UpdateProfileRequest
{
    // Organizers use FullName
    [StringLength(100)]
    public string? FullName { get; set; }

    // Participants use FirstName and LastName
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    // Participants only
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
}