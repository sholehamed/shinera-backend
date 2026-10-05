using Microsoft.AspNetCore.Authorization;
using Modules.System.Identity.Application.Authorization;
using OpenIddict.Abstractions;
using System.Security.Claims;
using Web.SharedKernel.Authorization;

namespace Modules.System.Identity.Web.Authorization;

public sealed class PermissionAuthorizationHandler(
    IPermissionAuthorizationService permissionAuthorizationService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var subject = context.User.FindFirstValue(
            OpenIddictConstants.Claims.Subject);

        if (!Guid.TryParse(subject, out var userId))
            return;

        var decision = await permissionAuthorizationService.HasPermissionAsync(
            userId,
            requirement.Resource,
            requirement.Action);

        if (decision.IsAllowed)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail(
            new AuthorizationFailureReason(
                this,
                ToErrorCode(decision.Code)));
    }

    private static string ToErrorCode(PermissionDecisionCode code) =>
        code switch
        {
            PermissionDecisionCode.TenantContextMissing =>
                "authorization.tenant_denied",

            PermissionDecisionCode.TenantMembershipRequired =>
                "authorization.tenant_denied",

            PermissionDecisionCode.ScopeDenied =>
                "authorization.scope_denied",

            _ => "authorization.permission_required"
        };
}
