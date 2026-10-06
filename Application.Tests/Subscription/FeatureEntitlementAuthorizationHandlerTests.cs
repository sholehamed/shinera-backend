using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Modules.System.Subscription.Application.Entitlements;
using Modules.System.Subscription.Web.Authorization;
using Web.SharedKernel.Authorization;

namespace Application.Tests.Subscription;

public sealed class FeatureEntitlementAuthorizationHandlerTests
{
    [Fact]
    public async Task EnabledFeature_SucceedsRequirement()
    {
        var requirement = new FeatureRequirement(
            FeatureKeys.AdvancedReports);

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                    "test")),
            null);

        var handler =
            new FeatureEntitlementAuthorizationHandler(
                new StubEntitlementService(
                    EntitlementDecision.Allow()));

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task MissingFeature_FailsWithStableSubscriptionCode()
    {
        var requirement = new FeatureRequirement(
            FeatureKeys.AdvancedReports);

        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                    "test")),
            null);

        var handler =
            new FeatureEntitlementAuthorizationHandler(
                new StubEntitlementService(
                    EntitlementDecision.Deny(
                        "subscription.feature_unavailable")));

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.Contains(
            context.FailureReasons,
            reason =>
                reason.Message ==
                "subscription.feature_unavailable");
    }

    private sealed class StubEntitlementService(
        EntitlementDecision decision)
        : IEntitlementService
    {
        public Task<EntitlementDecision> HasFeatureAsync(
            string featureKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(decision);

        public Task<LimitDecision> CheckLimitAsync(
            string limitKey,
            int currentUsage,
            int requestedIncrease = 1,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<EffectiveEntitlementsDto>
            GetEffectiveEntitlementsAsync(
                CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
