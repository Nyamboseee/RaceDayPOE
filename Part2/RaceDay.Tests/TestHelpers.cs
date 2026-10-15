using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RaceDay.Tests;

public static class TestHelpers
{
    public const string Password = "Password123!";

    // A client with its own cookie jar, so each client is its own "browser"
    public static HttpClient NewClient(RaceDayFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    // Registers a fresh user with a unique email, logs in, and returns the client
    public static async Task<HttpClient> LoggedInClient(RaceDayFactory factory, string role)
    {
        var email = $"{Guid.NewGuid():N}@test.com";
        var client = NewClient(factory);

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            role,
            firstName = "Test",
            lastName = role,
            email,
            password = Password
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();

        return client;
    }
}