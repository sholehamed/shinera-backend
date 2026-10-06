using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Identity.Web.Authorization;
using Modules.System.Subscription.Application.Entitlements;
using Modules.System.Subscription.Web.Authorization;
using OpenIddict.Abstractions;
using Web.SharedKernel.Authorization;

namespace Application.Tests.Subscription;

public sealed class PermissionEntitlementMatrixTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    public async Task PremiumOperation_RequiresPermissionAndEntitlement(
        bool hasPermission,
        bool hasFeature,
        bool expectedAllowed)
    {
        var userId = Guid.NewGuid();

        var permissionRequirement =
            new PermissionRequirement(
                "reports",
                "view");

        var featureRequirement =
            new FeatureRequirement(
                FeatureKeys.AdvancedReports);

        var identity = new ClaimsIdentity(
            authenticationType: "test");

        identity.AddClaim(new Claim(
            OpenIddictConstants.Claims.Subject,
            userId.ToString()));

        var context = new AuthorizationHandlerContext(
            [permissionRequirement, featureRequirement],
            new ClaimsPrincipal(identity),
            null);

        var permissionHandler =
            new PermissionAuthorizationHandler(
                new StubPermissionAuthorizationService(
                    hasPermission));

        var featureHandler =
            new FeatureEntitlementAuthorizationHandler(
                new StubEntitlementService(
                    hasFeature));

        await permissionHandler.HandleAsync(context);
        await featureHandler.HandleAsync(context);

        Assert.Equal(
            expectedAllowed,
            context.HasSucceeded);

        if (!expectedAllowed)
            Assert.True(context.HasFailed);
    }

    private sealed class StubPermissionAuthorizationService(
        bool hasPermission)
        : IPermissionAuthorizationService
    {
        public Task<PermissionDecision> HasPermissionAsync(
            Guid userId,
            string resource,
            string action,
            CancellationToken cancellationToken = default)
        {
            var key =
                $"{resource.Trim().ToLowerInvariant()}.{action.Trim().ToLowerInvariant()}";

            return Task.FromResult(
                hasPermission
                    ? PermissionDecision.Allow(
                        key,
                        PermissionScopeType.Tenant)
                    : PermissionDecision.Deny(
                        key,
                        PermissionDecisionCode.PermissionRequired));
        }

        public Task<PermissionDecision> AuthorizeAsync(
            Guid userId,
            string resource,
            string action,
            PermissionScopeContext resourceContext,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<EffectivePermissionDto>>
            GetGrantedScopesAsync(
                Guid userId,
                string resource,
                string action,
                CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<EffectivePermissionDto>>
            GetEffectivePermissionsAsync(
                Guid userId,
                CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubEntitlementService(
        bool hasFeature)
        : IEntitlementService
    {
        public Task<EntitlementDecision> HasFeatureAsync(
            string featureKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                hasFeature
                    ? EntitlementDecision.Allow()
                    : EntitlementDecision.Deny(
                        "subscription.feature_unavailable"));

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
