
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RaceDay.API.Tests;

public class OrganiserAuthorizationTests
{
    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        await using var factory = new RaceDayTestFactory();

        await factory.SeedParticipantRoleAsync();
        await factory.SeedOrganiserAndVenueAsync();

        var (email, password) = await factory.CreateOrganiserAsync();

        using var client = factory.CreateClient();

        // Log in as the Organiser.
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

        // Create an event using the Organiser token.
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var eventRequest = new
        {
            EventName = "Automated Test Race",
            Description = "Created by an automated authorization test.",
            EventDate = DateTime.UtcNow.Date.AddDays(30),
            StartTime = "09:00:00",
            RegistrationDeadline = DateTime.UtcNow.AddDays(20),
            VenuesID = 1
        };

        var response = await client.PostAsJsonAsync(
            "/api/Events", eventRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
