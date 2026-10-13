namespace RaceDay.Api.Models;

public class Enrollment
{
    public int EnrollmentId { get; set; }
    public int ParticipantId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.Now;
    public string BibNumber { get; set; } = string.Empty;

    public Participant? Participant { get; set; }
    public Event? Event { get; set; }
    public Category? Category { get; set; }
    public Result? Result { get; set; }
}