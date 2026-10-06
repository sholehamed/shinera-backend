using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntegrationTests;

public sealed class AuthenticationRateLimitTests(
    WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Login_IsRateLimitedPerClient()
    {
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        HttpStatusCode? lastStatus = null;

        for (var attempt = 0; attempt < 11; attempt++)
        {
            var response = await client.PostAsJsonAsync(
                "/System/Auth/session/login",
                new
                {
                    identifier = "nobody@example.test",
                    password = "invalid",
                    captchaToken = "invalid",
                    captchaCode = "invalid"
                });

            lastStatus = response.StatusCode;
        }

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            lastStatus);
    }
}
