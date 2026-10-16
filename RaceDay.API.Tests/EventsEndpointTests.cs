
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RaceDay.API.Tests;

public class EventsEndpointTests
{
    [Fact]
    public async Task GetEvents_ReturnsOk_WithoutToken()
    {
        // Arrange
        await using var factory = new RaceDayTestFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/Events");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}


