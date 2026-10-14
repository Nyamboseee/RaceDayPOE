using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class EventRequest
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(0.1, 1000)]
    public decimal Distance { get; set; }

    [Required]
    public string EventType { get; set; } = string.Empty; // Run, Walk or Cycle
}