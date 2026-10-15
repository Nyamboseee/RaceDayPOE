using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

public class EventAndEnrollmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventAndEnrollmentTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static object NewEvent(string name = "Test 10km") => new
    {
        eventName = name,
        description = "A test event",
        eventDate = "2026-12-01T06:00:00",
        location = "Durban",
        distance = 10,
        eventType = "Run"
    };

    private static async Task<int> CreateEvent(HttpClient organizer)
    {
        var response = await organizer.PostAsJsonAsync("/api/events", NewEvent());
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    private static async Task<int> CreateCategory(HttpClient organizer, int eventId)
    {
        var response = await organizer.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "Senior", distance = 10, minAge = 18, maxAge = 99 });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    // ---------- Event management ----------

    [Fact]
    public async Task Organizer_CanCreateEvent()
    {
        var organizer = await TestHelpers.LoggedInClient(_factory, "Organizer");

        var response = await organizer.PostAsJsonAsync("/api/events", NewEvent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CanUpdateOwnEvent()
    {
        var organizer = await TestHelpers.LoggedInClient(_factory, "Organizer");
        var id = await CreateEvent(organizer);

        var response = await organizer.PutAsJsonAsync($"/api/events/{id}", NewEvent("Renamed Event"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CanDeleteOwnEvent()
    {
        var organizer = await TestHelpers.LoggedInClient(_factory, "Organizer");
        var id = await CreateEvent(organizer);

        var response = await organizer.DeleteAsync($"/api/events/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidType_ReturnsBadRequest()
    {
        var organizer = await TestHelpers.LoggedInClient(_factory, "Organizer");

        var response = await organizer.PostAsJsonAsync("/api/events", new
        {
            eventName = "Bad type",
            eventDate = "2026-12-01T06:00:00",
            location = "Durban",
            distance = 5,
            eventType = "Swim"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CannotUpdateAnotherOrganizersEvent()
    {
        var owner = await TestHelpers.LoggedInClient(_factory, "Organizer");
        var other = await TestHelpers.LoggedInClient(_factory, "Organizer");
        var id = await CreateEvent(owner);

        var response = await other.PutAsJsonAsync($"/api/events/{id}", NewEvent("Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BothRoles_CanViewEvents()
    {
        var organizer = await TestHelpers.LoggedInClient(_factory, "Organizer");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");

        Assert.Equal(HttpStatusCode.OK, (await organizer.GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await participant.GetAsync("/api/events")).StatusCode);
    }

    // ---------- Role rejection ----------

    [Fact]
    public async Task Participant_CannotCreateEvent_ReturnsForbidden()
    {
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/events", NewEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotDeleteEvent_ReturnsForbidden()
    {
        var organizer = await TestHelpers.LoggedInClient(_factory, "Organizer");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var id = await CreateEvent(organizer);

        var response = await participant.DeleteAsync($"/api/events/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CannotEnrol_ReturnsForbidden()
    {
        var organizer = await TestHelpers.LoggedInClient(_factory, "Organizer");
        var eventId = await CreateEvent(organizer);
        var categoryId = await CreateCategory(organizer, eventId);

        var response = await organizer.PostAsync($"/api/categories/{categoryId}/enrollments", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_ReturnsForbidden()
    {
        var organizer = await TestHelpers.LoggedInClient(_factory, "Organizer");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var eventId = await CreateEvent(organizer);

        var response = await participant.GetAsync($"/api/events/{eventId}/enrollments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedUser_CannotCreateEvent_ReturnsUnauthorized()
    {
        var anonymous = TestHelpers.NewClient(_factory);

        var response = await anonymous.PostAsJsonAsync("/api/events", NewEvent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedUser_CannotViewEvents_ReturnsUnauthorized()
    {
        var anonymous = TestHelpers.NewClient(_factory);

        var response = await anonymous.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Enrolments ----------

    [Fact]
    public async Task Participant_CanEnrol_AndTheEnrolmentIsRecorded()
    {
        var organizer = await TestHelpers.LoggedInClient(_factory, "Organizer");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var eventId = await CreateEvent(organizer);
        var categoryId = await CreateCategory(organizer, eventId);

        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrollments", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // The link between participant, event and category is stored in the database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var saved = await db.Enrollments.SingleAsync(e => e.CategoryId == categoryId);
        Assert.Equal(eventId, saved.EventId);
        Assert.False(string.IsNullOrWhiteSpace(saved.BibNumber));
    }

    [Fact]
    public async Task Participant_CannotEnrolTwiceInTheSameCategory_ReturnsConflict()
    {
        var organizer = await TestHelpers.LoggedInClient(_factory, "Organizer");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var eventId = await CreateEvent(organizer);
        var categoryId = await CreateCategory(organizer, eventId);
        await participant.PostAsync($"/api/categories/{categoryId}/enrollments", null);

        var second = await participant.PostAsync($"/api/categories/{categoryId}/enrollments", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_InUnknownCategory_ReturnsNotFound()
    {
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");

        var response = await participant.PostAsync("/api/categories/99999/enrollments", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CanViewEnrolmentsForOwnEvent()
    {
        var organizer = await TestHelpers.LoggedInClient(_factory, "Organizer");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var eventId = await CreateEvent(organizer);
        var categoryId = await CreateCategory(organizer, eventId);
        await participant.PostAsync($"/api/categories/{categoryId}/enrollments", null);

        var response = await organizer.GetAsync($"/api/events/{eventId}/enrollments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, list.GetArrayLength());
    }

    [Fact]
    public async Task Participant_SeesOnlyTheirOwnEnrolments()
    {
        var organizer = await TestHelpers.LoggedInClient(_factory, "Organizer");
        var first = await TestHelpers.LoggedInClient(_factory, "Participant");
        var second = await TestHelpers.LoggedInClient(_factory, "Participant");
        var eventId = await CreateEvent(organizer);
        var categoryId = await CreateCategory(organizer, eventId);
        await first.PostAsync($"/api/categories/{categoryId}/enrollments", null);

        var mine = await first.GetFromJsonAsync<JsonElement>("/api/enrollments/me");
        var theirs = await second.GetFromJsonAsync<JsonElement>("/api/enrollments/me");

        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(0, theirs.GetArrayLength());
    }
}