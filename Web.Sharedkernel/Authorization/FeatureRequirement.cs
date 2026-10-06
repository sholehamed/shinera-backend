using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace Web.SharedKernel.Authorization;

public sealed record FeatureRequirement(
    string FeatureKey) : IAuthorizationRequirement;

public static class FeatureEndpointConventionBuilderExtensions
{
    public static TBuilder RequireFeature<TBuilder>(
        this TBuilder builder,
        string featureKey)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureKey);

        return builder.RequireAuthorization(policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(
                new FeatureRequirement(
                    featureKey.Trim().ToLowerInvariant()));
        });
    }
}
