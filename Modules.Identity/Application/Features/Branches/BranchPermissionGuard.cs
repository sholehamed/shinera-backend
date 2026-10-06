using Application.SharedKernel.Exceptions;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Application.Features.Branches;

internal static class BranchPermissionGuard
{
    public static async Task RequireTenantScopeAsync(
        IPermissionAuthorizationService authorizationService,
        ITenantContext tenantContext,
        string action,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(tenantContext);

        var grants = await authorizationService.GetGrantedScopesAsync(
            userId,
            SystemPermissionCatalog.Branches.Resource,
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

    public static async Task RequireBranchScopeAsync(
        IPermissionAuthorizationService authorizationService,
        ITenantContext tenantContext,
        string action,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(tenantContext);

        var decision = await authorizationService.AuthorizeAsync(
            userId,
            SystemPermissionCatalog.Branches.Resource,
            action,
            new PermissionScopeContext(
                BranchId: branchId,
                BranchAccessValidated:
                    tenantContext.ReadableBranchIds.Contains(branchId)),
            cancellationToken);

        if (!decision.IsAllowed)
            throw new ForbiddenAccessException();
    }

    public static async Task<Guid[]?> ResolveVisibleBranchIdsAsync(
        IPermissionAuthorizationService authorizationService,
        ITenantContext tenantContext,
        string action,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(tenantContext);

        var grants = await authorizationService.GetGrantedScopesAsync(
            userId,
            SystemPermissionCatalog.Branches.Resource,
            action,
            cancellationToken);

        if (grants.Any(x =>
                string.Equals(
                    x.Scope,
                    PermissionScopeType.Tenant.ToString(),
                    StringComparison.Ordinal)))
        {
            return null;
        }

        var branchGrants = grants
            .Where(x =>
                string.Equals(
                    x.Scope,
                    PermissionScopeType.Branch.ToString(),
                    StringComparison.Ordinal))
            .ToArray();

        if (branchGrants.Length == 0)
            throw new ForbiddenAccessException();

        var readableBranchIds =
            tenantContext.ReadableBranchIds.ToHashSet();

        if (branchGrants.Any(x => !x.ScopeReferenceId.HasValue))
            return readableBranchIds.Order().ToArray();

        return branchGrants
            .Where(x =>
                x.ScopeReferenceId.HasValue &&
                readableBranchIds.Contains(
                    x.ScopeReferenceId.Value))
            .Select(x => x.ScopeReferenceId!.Value)
            .Distinct()
            .Order()
            .ToArray();
    }

    private static Guid GetUserId(
        ITenantContext tenantContext) =>
        tenantContext.UserId
        ?? throw new TenantAccessException(
            "tenant.user_context_invalid",
            "The authenticated user context is invalid.");
}
