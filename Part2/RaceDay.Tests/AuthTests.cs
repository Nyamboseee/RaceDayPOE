using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static object NewUser(string role, string email, string password = TestHelpers.Password) => new
    {
        role,
        firstName = "Test",
        lastName = "User",
        email,
        password
    };

    // ---------- Registration ----------

    [Fact]
    public async Task Register_Participant_ReturnsCreated()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser("Participant", "new.participant@test.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organizer_ReturnsCreated()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser("Organizer", "new.organizer@test.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = TestHelpers.NewClient(_factory);
        await client.PostAsJsonAsync("/api/auth/register", NewUser("Participant", "duplicate@test.com"));

        var second = await client.PostAsJsonAsync("/api/auth/register", NewUser("Participant", "duplicate@test.com"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_UnknownRole_ReturnsBadRequest()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser("Spectator", "spectator@test.com"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser("Participant", "short.pw@test.com", "abc"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotThePlainText()
    {
        var client = TestHelpers.NewClient(_factory);
        await client.PostAsJsonAsync("/api/auth/register", NewUser("Participant", "hashed@test.com"));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var participant = await db.Participants.SingleAsync(p => p.Email == "hashed@test.com");

        Assert.NotEqual(TestHelpers.Password, participant.PasswordHash);
        Assert.False(string.IsNullOrWhiteSpace(participant.PasswordHash));
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsOkWithRole()
    {
        var client = TestHelpers.NewClient(_factory);
        await client.PostAsJsonAsync("/api/auth/register", NewUser("Organizer", "login.ok@test.com"));

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "login.ok@test.com", password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Organizer", body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = TestHelpers.NewClient(_factory);
        await client.PostAsJsonAsync("/api/auth/register", NewUser("Participant", "login.wrong@test.com"));

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "login.wrong@test.com", password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@test.com", password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Session ----------

    [Fact]
    public async Task Me_WithoutLogin_ReturnsUnauthorized()
    {
        var client = TestHelpers.NewClient(_factory);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_AfterLogin_ReturnsTheSessionRole()
    {
        var client = await TestHelpers.LoggedInClient(_factory, "Participant");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Participant", body.GetProperty("role").GetString());
    }
}