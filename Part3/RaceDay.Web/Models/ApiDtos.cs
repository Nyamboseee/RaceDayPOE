namespace RaceDay.Web.Models;

// These classes match the JSON the Part 2 API sends back

public record EventDto(
    int Id, string Name, string? Description, DateTime Date, string Location,
    decimal Distance, string EventType, int OrganizerId, string? BannerUrl,
    int EnrollmentCount, int CategoryCount);

public record CategoryDto(int Id, int EventId, string Name, decimal Distance, int? MinAge, int? MaxAge);

public record MyEnrollmentDto(
    int Id, int EventId, string EventName, DateTime EventDate, int CategoryId,
    string CategoryName, string BibNumber, string Status, DateTime EnrolledOn);

public record EventEnrollmentDto(
    int Id, int ParticipantId, string ParticipantName, string CategoryName, string BibNumber,
    string Status, DateTime EnrolledOn, int? ResultId, string? ResultStatus,
    TimeSpan? FinishTime, int? Position);

public record MyResultDto(
    int Id, string EventName, DateTime EventDate, string CategoryName, string BibNumber,
    TimeSpan? FinishTime, int? Position, string Status, int TotalFinishers);

public record LoginResponseDto(int Id, string Name, string Email, string Role);

public class ProfileDto
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
