
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.API.Tests;

public class AuthEndpointTests
{
    [Fact]
    public async Task Register_WithValidDetails_ReturnsCreated()
    {
        // Arrange
        await using var factory = new RaceDayTestFactory();

        await factory.SeedParticipantRoleAsync();

        using var client = factory.CreateClient();

        var registration = new
        {
            // Use a unique email for every test run.
            Email = $"test{Guid.NewGuid():N}@example.com",
            Password = "TestPassword123!",
            FirstName = "Test",
            LastName = "Participant"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/Auth/register",
            registration);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory =
            new RaceDayTestFactory();

        using var client = factory.CreateClient();

        var loginRequest = new
        {
            Email = $"missing{Guid.NewGuid():N}@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/Auth/login",
            loginRequest);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task RegisterThenLogin_ReturnsAccessToken()
    {
        // Arrange
        await using var factory = new RaceDayTestFactory();

        await factory.SeedParticipantRoleAsync();

        using var client = factory.CreateClient();

        var email = $"login{Guid.NewGuid():N}@example.com";
        const string password = "TestPassword123!";

        var registration = new
        {
            FirstName = "Test",
            LastName = "Participant",
            Email = email,
            Password = password,
            PhoneNumber = "0712345678"
        };

        // Act: Register
        var registerResponse = await client.PostAsJsonAsync(
            "/api/Auth/register", registration);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        // Act: Login
        var loginRequest = new
        {
            Email = email,
            Password = password
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/Auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginData = await loginResponse.Content
            .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginData);
        Assert.False(string.IsNullOrWhiteSpace(loginData.AccessToken));
        Assert.Equal("Bearer", loginData.TokenType);
    }

    private sealed class LoginResponse
    {
        public string? AccessToken { get; set; }
        public string? TokenType { get; set; }
    }
    
[Fact]
public async Task Register_AsOrganiser_ReturnsOrganiserRole()
    {
        await using var factory = new RaceDayTestFactory();

        await factory.SeedParticipantRoleAsync();
        await factory.SeedOrganiserAndVenueAsync();

        using var client = factory.CreateClient();

        var email = $"organiser{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync(
            "/api/Auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Organiser",
                Email = email,
                Password = "TestPassword123!",
                PhoneNumber = "0712345678",
                Role = "Organiser"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var returnedRole = json.RootElement
            .GetProperty("role")
            .GetString();

        Assert.Equal("Organiser", returnedRole);
    }

[Fact]
public async Task Register_AsParticipant_ReturnsParticipantRole()
    {
        await using var factory = new RaceDayTestFactory();

        await factory.SeedParticipantRoleAsync();

        using var client = factory.CreateClient();

        var email = $"participant{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync(
            "/api/Auth/register",
            new
            {
                FirstName = "Test",
                LastName = "Participant",
                Email = email,
                Password = "TestPassword123!",
                PhoneNumber = "0712345678",
                Role = "Participant"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var returnedRole = json.RootElement
            .GetProperty("role")
            .GetString();

        Assert.Equal("Participant", returnedRole);
    }

}

