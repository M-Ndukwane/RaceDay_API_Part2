
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RaceDay.API.Tests;

public class AuthorizationEndpointTests
{
    [Fact]
    public async Task Participant_CannotCreateEvent()
    {
        // Arrange: use the isolated test database.
        await using var factory = new RaceDayTestFactory();
        await factory.SeedParticipantRoleAsync();

        using var client = factory.CreateClient();

        var email = $"participant{Guid.NewGuid():N}@example.com";
        const string password = "TestPassword123!";

        // Register a Participant.
        var registration = new
        {
            FirstName = "Test",
            LastName = "Participant",
            Email = email,
            Password = password,
            PhoneNumber = "0712345678"
        };

        var registerResponse = await client.PostAsJsonAsync(
            "/api/Auth/register", registration);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        // Log in to obtain a JWT token.
        var loginResponse = await client.PostAsJsonAsync(
            "/api/Auth/login",
            new { Email = email, Password = password });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var loginJson = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        var token = loginJson.RootElement
            .GetProperty("accessToken")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        // Act: attempt to create an event as a Participant.
        using var request = new HttpRequestMessage(
            HttpMethod.Post, "/api/Events");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = new StringContent(
            "{}", Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        // Assert: only Organisers may create events.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    
[Fact]
public async Task Participant_CannotDeleteEvent()
    {
        await using var factory = new RaceDayTestFactory();

        await factory.SeedParticipantRoleAsync();

        using var client = factory.CreateClient();

        var email = $"participant{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/Auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Participant",
                Email = email,
                Password = "TestPassword123!",
                PhoneNumber = "0712345678"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                Email = email,
                Password = "TestPassword123!"
            });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        using var json = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        var token = json.RootElement
            .GetProperty("accessToken")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", token);

        var response = await client.DeleteAsync("/api/Events/1");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
    
[Fact]
public async Task Participant_CannotUpdateEvent()
    {
        await using var factory = new RaceDayTestFactory();

        await factory.SeedParticipantRoleAsync();

        using var client = factory.CreateClient();

        var email = $"participant{Guid.NewGuid():N}@example.com";
        const string password = "TestPassword123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/Auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Participant",
                Email = email,
                Password = password,
                PhoneNumber = "0712345678"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                Email = email,
                Password = password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        using var json = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        var token = json.RootElement
            .GetProperty("accessToken")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", token);

        var response = await client.PutAsJsonAsync(
            "/api/Events/1",
            new { });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}

