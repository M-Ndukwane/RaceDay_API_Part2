
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.API.Tests;

public class ResultsEndpointTests
{
    [Fact]
    public async Task GetResults_ReturnsUnauthorized_WithoutToken()
    {
        // Arrange
        await using var factory =
           new RaceDayTestFactory();

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/Results");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    
[Fact]
public async Task Participant_CannotCreateResult()
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

        var response = await client.PostAsJsonAsync(
            "/api/Results",
            new { });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}
