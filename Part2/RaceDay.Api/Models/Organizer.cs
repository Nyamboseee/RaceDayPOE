namespace RaceDay.Api.Models;

public class Organizer
{
    public int OrganizerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? ContactNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}