using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Web.SharedKernel.Authorization;

namespace IntegrationTests;

public sealed class EndpointAuthorizationMetadataTests(
    WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly string[] PermissionProtectedPrefixes =
    [
        "/System/Tenants",
        "/System/Modules",
        "/System/Resources",
        "/System/Users",
        "/System/Roles",
        "/System/Permissions",
        "/System/ApiResources",
        "/System/UiResources",
        "/System/Groups",
        "/System/Menus",
        "/System/MenuCategories",
        "/System/Dashboard"
    ];

    [Fact]
    public void SystemEndpoints_AreAnonymousOnlyWhenExplicit_OtherwiseAuthorized()
    {
        using var client = factory.CreateClient();

        var endpoints = GetRouteEndpoints()
            .Where(endpoint =>
                NormalizeRoute(endpoint)
                    .StartsWith(
                        "/System/",
                        StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(endpoints);

        foreach (var endpoint in endpoints)
        {
            var route = NormalizeRoute(endpoint);

            var isAnonymous =
                endpoint.Metadata.GetMetadata<IAllowAnonymous>()
                is not null;

            var hasAuthorization =
                endpoint.Metadata
                    .GetOrderedMetadata<IAuthorizeData>()
                    .Count > 0
                ||
                endpoint.Metadata
                    .GetOrderedMetadata<AuthorizationPolicy>()
                    .Count > 0;

            Assert.True(
                isAnonymous || hasAuthorization,
                $"Endpoint '{route}' has neither explicit anonymous nor authorization metadata.");
        }
    }

    [Fact]
    public void AdministrativeEndpoints_HavePermissionRequirements()
    {
        using var client = factory.CreateClient();

        var endpoints = GetRouteEndpoints()
            .Where(endpoint =>
            {
                var route = NormalizeRoute(endpoint);

                return PermissionProtectedPrefixes.Any(
                    prefix =>
                        route.StartsWith(
                            prefix,
                            StringComparison.OrdinalIgnoreCase));
            })
            .ToList();

        Assert.NotEmpty(endpoints);

        foreach (var endpoint in endpoints)
        {
            var route = NormalizeRoute(endpoint);

            if (route.Equals(
                    "/System/Tenants/resolve",
                    StringComparison.OrdinalIgnoreCase)
                ||
                route.Equals(
                    "/System/Menus/getUserMenus",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var permissionRequirements =
                endpoint.Metadata
                    .GetOrderedMetadata<AuthorizationPolicy>()
                    .SelectMany(policy => policy.Requirements)
                    .OfType<PermissionRequirement>()
                    .ToList();

            Assert.NotEmpty(permissionRequirements);
        }
    }

    private IReadOnlyList<RouteEndpoint> GetRouteEndpoints()
    {
        return factory.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();
    }

    private static string NormalizeRoute(
        RouteEndpoint endpoint)
    {
        var raw = endpoint.RoutePattern.RawText ?? string.Empty;

        return "/" + raw.TrimStart('/');
    }
}
