using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Workforce.Application.Authorization;

internal static class SchedulePermissionGuard
{
    public static async Task RequireTenantScopeAsync(
        IPermissionAuthorizationService authorizationService,
        ICurrentTenant currentTenant,
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

        var grants = await authorizationService.GetGrantedScopesAsync(
            userId,
            SystemPermissionCatalog.Schedules.Resource,
            action,
            cancellationToken);

        if (!grants.Any(x =>
                string.Equals(
                    x.Scope,
                    PermissionScopeType.Tenant.ToString(),
                    StringComparison.Ordinal)))
        {
            throw new ForbiddenAccessException();
        }
    }
}
