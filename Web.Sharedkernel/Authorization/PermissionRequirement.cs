using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace Web.SharedKernel.Authorization;

public sealed record PermissionRequirement(
    string Resource,
    string Action) : IAuthorizationRequirement
{
    public string Key =>
        $"{Resource.Trim().ToLowerInvariant()}.{Action.Trim().ToLowerInvariant()}";
}

public static class PermissionEndpointConventionBuilderExtensions
{
    public static TBuilder RequirePermission<TBuilder>(
        this TBuilder builder,
        string resource,
        string action)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        return builder.RequireAuthorization(policy =>
            policy.AddRequirements(
                new PermissionRequirement(resource, action)));
    }
}
