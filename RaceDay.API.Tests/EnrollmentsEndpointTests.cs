
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RaceDay.API.Tests;

public class EnrollmentsEndpointTests
{
    [Fact]
    public async Task GetEnrollments_ReturnsUnauthorized_WithoutToken()
    {
        // Arrange
        await using var factory = new RaceDayTestFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/Enrollments");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}
