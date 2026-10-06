using Modules.System.Subscription.Application.Entitlements;
using Web.SharedKernel.Authorization;

namespace Modules.System.Subscription.Web.Authorization;

public sealed class FeatureEntitlementAuthorizationHandler(
    IEntitlementService entitlementService)
    : AuthorizationHandler<FeatureRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FeatureRequirement requirement)
    {
        var decision = await entitlementService.HasFeatureAsync(
            requirement.FeatureKey);

        if (decision.IsAllowed)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail(
            new AuthorizationFailureReason(
                this,
                decision.ErrorCode ??
                "subscription.feature_unavailable"));
    }
}
