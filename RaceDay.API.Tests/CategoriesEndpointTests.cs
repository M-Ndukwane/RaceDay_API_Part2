
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.API.Tests;

public class CategoriesEndpointTests
{
    [Fact]
    public async Task GetCategories_ReturnsOk_WithoutToken()
    {
        // Arrange
        await using var factory =
            new RaceDayTestFactory();

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/Categories");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
[Fact]
public async Task Participant_CannotCreateCategory()
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

        // A Participant must not be allowed to create a Category.
        var response = await client.PostAsJsonAsync(
            "/api/Categories",
            new { });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}

