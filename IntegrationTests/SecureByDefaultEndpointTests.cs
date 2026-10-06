using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntegrationTests;

public sealed class SecureByDefaultEndpointTests(
    WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/System/Tenants/pagedList")]
    [InlineData("/System/Branches")]
    [InlineData("/System/BusinessProfiles")]
    [InlineData("/System/Workspace/current")]
    [InlineData("/System/Users/pagedList")]
    [InlineData("/System/Roles/lookup")]
    [InlineData("/System/Resources/lookup")]
    [InlineData("/System/Permissions/lookup")]
    [InlineData("/Subscription/Entitlements/current")]
    [InlineData("/Services/ServiceCategories")]
    [InlineData("/Services/Services")]
    [InlineData("/Workforce/Staff")]
    [InlineData("/Workforce/Staff/11111111-1111-1111-1111-111111111111/schedule")]
    [InlineData("/Crm/Customers")]
    public async Task ManagedEndpoints_WithoutAuthentication_ReturnUnauthorized(
        string path)
    {
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        using var request = path.EndsWith(
            "/pagedList",
            StringComparison.Ordinal)
                ? new HttpRequestMessage(
                    HttpMethod.Post,
                    path)
                {
                    Content = JsonContent.Create(new { })
                }
                : new HttpRequestMessage(
                    HttpMethod.Get,
                    path);

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CaptchaEndpoint_RemainsExplicitlyAnonymous()
    {
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        var response = await client.GetAsync(
            "/System/Auth/captcha/new");

        Assert.NotEqual(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
        Assert.NotEqual(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}
