using Application.SharedKernel.Exceptions;
using Modules.System.Identity.Application.Authorization;

namespace Modules.System.Services.Application.Authorization;

internal static class ServiceCatalogPermissionGuard
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
                "A tenant context is required for service catalog operations.");
        }

        var grants = await authorizationService.GetGrantedScopesAsync(
            userId,
            SystemPermissionCatalog.Services.Resource,
            action,
            cancellationToken);

        if (!grants.Any(x =>
                string.Equals(
                    x.Scope,
                    "Tenant",
                    StringComparison.Ordinal)))
        {
            throw new ForbiddenAccessException();
        }
    }
}
