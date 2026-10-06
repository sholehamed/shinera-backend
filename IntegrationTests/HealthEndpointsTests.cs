using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntegrationTests;

public sealed class HealthEndpointsTests(
    WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_ReturnsSuccess(string path)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
