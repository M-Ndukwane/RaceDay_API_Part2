
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.API.Tests;

public class UserAuthorizationTests
{
    [Fact]
    public async Task Participant_CannotAccessAnotherUsersProfile()
    {
        // Arrange: use the isolated test database.
        await using var factory = new RaceDayTestFactory();
        await factory.SeedParticipantRoleAsync();

        using var client = factory.CreateClient();

        const string password = "TestPassword123!";

        // Register the first Participant.
        var firstEmail = $"first{Guid.NewGuid():N}@example.com";

        var firstRegistration = await client.PostAsJsonAsync(
            "/api/Auth/register",
            new
            {
                FirstName = "First",
                LastName = "Participant",
                Email = firstEmail,
                Password = password,
                PhoneNumber = "0712345678"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            firstRegistration.StatusCode);

        // Register the second Participant.
        var secondEmail = $"second{Guid.NewGuid():N}@example.com";

        var secondRegistration = await client.PostAsJsonAsync(
            "/api/Auth/register",
            new
            {
                FirstName = "Second",
                LastName = "Participant",
                Email = secondEmail,
                Password = password,
                PhoneNumber = "0712345679"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            secondRegistration.StatusCode);

        // Read the second user's ID from the registration response.
        using var secondJson = JsonDocument.Parse(
            await secondRegistration.Content.ReadAsStringAsync());

        var secondUserId = secondJson.RootElement
    .GetProperty("usersID")
    .GetInt32();

        // Log in as the first Participant.
        var loginResponse = await client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                Email = firstEmail,
                Password = password
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var loginJson = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        var token = loginJson.RootElement
            .GetProperty("accessToken")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        // Act: request the second Participant's profile.
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/Users/{secondUserId}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);

        // Assert: access must be forbidden.
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanAccessOwnProfile()
    {
        await using var factory = new RaceDayTestFactory();
        await factory.SeedParticipantRoleAsync();

        using var client = factory.CreateClient();

        var email = $"self{Guid.NewGuid():N}@example.com";
        const string password = "TestPassword123!";

        // Register the Participant.
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

        using var registrationJson = JsonDocument.Parse(
            await registerResponse.Content.ReadAsStringAsync());

        var userId = registrationJson.RootElement
            .GetProperty("usersID")
            .GetInt32();

        // Log in.
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

        // Request the user's own profile.
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"/api/Users/{userId}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanAccessAnotherUsersProfile()
    {
        await using var factory = new RaceDayTestFactory();

        await factory.SeedParticipantRoleAsync();

        var (organiserEmail, organiserPassword) =
            await factory.CreateOrganiserAsync();

        using var client = factory.CreateClient();

        // Register a Participant.
        var participantEmail =
            $"participant{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/Auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Participant",
                Email = participantEmail,
                Password = "TestPassword123!",
                PhoneNumber = "0712345678"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        using var registrationJson = JsonDocument.Parse(
            await registerResponse.Content.ReadAsStringAsync());

        var participantId = registrationJson.RootElement
            .GetProperty("usersID")
            .GetInt32();

        // Log in as the Organiser.
        var loginResponse = await client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                Email = organiserEmail,
                Password = organiserPassword
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var loginJson = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        var token = loginJson.RootElement
            .GetProperty("accessToken")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        // Request the Participant's profile as an Organiser.
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/Users/{participantId}");

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", token);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

[Fact]
public async Task UnauthenticatedUser_CannotAccessProfile()
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

        using var json = JsonDocument.Parse(
            await registerResponse.Content.ReadAsStringAsync());

        var participantId = json.RootElement
            .GetProperty("usersID")
            .GetInt32();

        // Access the profile without supplying a token.
        var response = await client.GetAsync(
            $"/api/Users/{participantId}");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}

