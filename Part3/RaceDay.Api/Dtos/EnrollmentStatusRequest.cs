using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class EnrollmentStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty; // Pending or Confirmed
}
