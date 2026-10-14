namespace RaceDay.Api.Auth;

public static class Roles
{
    public const string Organizer = "Organizer";
    public const string Participant = "Participant";
}

public static class Policies
{
    public const string OrganizerOnly = "OrganizerOnly";
    public const string ParticipantOnly = "ParticipantOnly";
}