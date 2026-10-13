namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }
    public int OrganizerId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty; // Run, Walk or Cycle

    public Organizer? Organizer { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}