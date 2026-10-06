using Modules.System.Identity.Application.Authorization;

namespace Modules.System.Appointments.Application.Authorization;

internal static class AppointmentPermissionGuard
{
    public static async Task RequireBranchAccessAsync(
        IPermissionAuthorizationService authorizationService,
        ICurrentTenant currentTenant,
        Guid branchId,
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
                "A tenant context is required for appointment operations.");
        }

        var branchAccessValidated =
            currentTenant.WritableBranchIds.Contains(branchId);

        var decision = await authorizationService.AuthorizeAsync(
            userId,
            SystemPermissionCatalog.Appointments.Resource,
            action,
            new PermissionScopeContext(
                branchId,
                branchAccessValidated),
            cancellationToken);

        if (!decision.IsAllowed)
        {
            throw new ForbiddenAccessException();
        }
    }
}
