using Modules.System.Identity.Application.Authorization;

namespace Modules.System.Workforce.Application.Authorization;

internal static class SchedulePermissionGuard
{
    public static async Task RequireAccessAsync(
        IPermissionAuthorizationService authorizationService,
        ICurrentTenant currentTenant,
        Guid? staffUserId,
        string action,
        CancellationToken cancellationToken)
    {
        var userId = currentTenant.UserId
            ?? throw new TenantAccessException(
                "tenant.user_context_invalid",
                "The authenticated user context is invalid.");

        if (!currentTenant.TenantId.HasValue)
        {
            throw new TenantAccessException(
                "tenant.context_missing",
                "A tenant context is required for schedule operations.");
        }

        var decision = await authorizationService.AuthorizeAsync(
            userId,
            SystemPermissionCatalog.Staff.Resource,
            action,
            new PermissionScopeContext(
                OwnerUserId: staffUserId),
            cancellationToken);

        if (!decision.IsAllowed)
            throw new ForbiddenAccessException();
    }
}
