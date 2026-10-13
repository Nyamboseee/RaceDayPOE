namespace RaceDay.Api.Models;

public class Result
{
    public int ResultId { get; set; }
    public int EnrollmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";

    public Enrollment? Enrollment { get; set; }
}