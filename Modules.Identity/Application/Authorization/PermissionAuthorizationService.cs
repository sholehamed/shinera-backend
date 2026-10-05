using Microsoft.EntityFrameworkCore;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Application.Authorization;

public sealed class PermissionAuthorizationService(
    IIdentityDbContext dbContext,
    ITenantContext tenantContext)
    : IPermissionAuthorizationService
{
    public async Task<PermissionDecision> HasPermissionAsync(
        Guid userId,
        string resource,
        string action,
        CancellationToken cancellationToken = default)
    {
        var resolution = await ResolveGrantsAsync(
            userId,
            resource,
            action,
            cancellationToken);

        if (resolution.Denial is not null)
            return resolution.Denial;

        var grant = resolution.Grants
            .OrderBy(x => ScopePriority(x.ScopeType))
            .First();

        return PermissionDecision.Allow(
            resolution.PermissionKey,
            grant.ScopeType);
    }

    public async Task<PermissionDecision> AuthorizeAsync(
        Guid userId,
        string resource,
        string action,
        PermissionScopeContext resourceContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resourceContext);

        var resolution = await ResolveGrantsAsync(
            userId,
            resource,
            action,
            cancellationToken);

        if (resolution.Denial is not null)
            return resolution.Denial;

        foreach (var grant in resolution.Grants
                     .OrderBy(x => ScopePriority(x.ScopeType)))
        {
            if (ScopeMatches(
                    userId,
                    grant.ScopeType,
                    grant.ScopeReferenceId,
                    resourceContext))
            {
                return PermissionDecision.Allow(
                    resolution.PermissionKey,
                    grant.ScopeType);
            }
        }

        return PermissionDecision.Deny(
            resolution.PermissionKey,
            PermissionDecisionCode.ScopeDenied);
    }

    public async Task<IReadOnlyList<EffectivePermissionDto>>
        GetGrantedScopesAsync(
            Guid userId,
            string resource,
            string action,
            CancellationToken cancellationToken = default)
    {
        var resolution = await ResolveGrantsAsync(
            userId,
            resource,
            action,
            cancellationToken);

        if (resolution.Denial is not null)
            return [];

        return resolution.Grants
            .OrderBy(x => ScopePriority(x.ScopeType))
            .Select(x => new EffectivePermissionDto(
                resolution.PermissionKey,
                x.ScopeType.ToString(),
                x.ScopeReferenceId))
            .Distinct()
            .ToArray();
    }

    public async Task<IReadOnlyList<EffectivePermissionDto>>
        GetEffectivePermissionsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.ActiveTenantId;
        if (!tenantId.HasValue)
            return [];

        var membershipExists = await dbContext.TenantMemberships
            .AsNoTracking()
            .AnyAsync(
                membership =>
                    membership.TenantId == tenantId.Value &&
                    membership.UserId == userId &&
                    membership.IsActive,
                cancellationToken);

        if (!membershipExists)
            return [];

        var roleIds = dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole =>
                userRole.TenantId == tenantId.Value &&
                userRole.UserId == userId &&
                userRole.Role.IsActive)
            .Select(userRole => userRole.RoleId);

        var grants = await dbContext.PermissionAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.TenantId == tenantId.Value &&
                assignment.IsActive &&
                assignment.Permission.IsActive &&
                assignment.Permission.Resource != null &&
                assignment.Permission.Resource.IsActive &&
                (
                    assignment.SubjectType == PermissionSubjectType.User &&
                    assignment.SubjectId == userId
                    ||
                    assignment.SubjectType == PermissionSubjectType.Role &&
                    roleIds.Contains(assignment.SubjectId)
                ))
            .Select(assignment => new
            {
                assignment.Permission.Code,
                assignment.ScopeType,
                assignment.ScopeReferenceId
            })
            .Distinct()
            .OrderBy(x => x.Code)
            .ThenBy(x => x.ScopeType)
            .ToListAsync(cancellationToken);

        return grants
            .Select(x => new EffectivePermissionDto(
                x.Code,
                x.ScopeType.ToString(),
                x.ScopeReferenceId))
            .ToArray();
    }

    private async Task<GrantResolution> ResolveGrantsAsync(
        Guid userId,
        string resource,
        string action,
        CancellationToken cancellationToken)
    {
        var permissionKey = NormalizePermissionKey(resource, action);

        var tenantId = tenantContext.ActiveTenantId;
        if (!tenantId.HasValue)
        {
            return GrantResolution.Denied(
                permissionKey,
                PermissionDecisionCode.TenantContextMissing);
        }

        var membershipExists = await dbContext.TenantMemberships
            .AsNoTracking()
            .AnyAsync(
                membership =>
                    membership.TenantId == tenantId.Value &&
                    membership.UserId == userId &&
                    membership.IsActive,
                cancellationToken);

        if (!membershipExists)
        {
            return GrantResolution.Denied(
                permissionKey,
                PermissionDecisionCode.TenantMembershipRequired);
        }

        var permissionId = await dbContext.Permissions
            .AsNoTracking()
            .Where(permission =>
                permission.IsActive &&
                permission.Resource != null &&
                permission.Resource.IsActive &&
                permission.Code == permissionKey)
            .Select(permission => (Guid?)permission.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (!permissionId.HasValue)
        {
            return GrantResolution.Denied(
                permissionKey,
                PermissionDecisionCode.PermissionRequired);
        }

        var roleIds = dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole =>
                userRole.TenantId == tenantId.Value &&
                userRole.UserId == userId &&
                userRole.Role.IsActive)
            .Select(userRole => userRole.RoleId);

        var grants = await dbContext.PermissionAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.TenantId == tenantId.Value &&
                assignment.IsActive &&
                assignment.PermissionId == permissionId.Value &&
                (
                    assignment.SubjectType == PermissionSubjectType.User &&
                    assignment.SubjectId == userId
                    ||
                    assignment.SubjectType == PermissionSubjectType.Role &&
                    roleIds.Contains(assignment.SubjectId)
                ))
            .Select(assignment => new PermissionGrant(
                assignment.ScopeType,
                assignment.ScopeReferenceId))
            .Distinct()
            .ToListAsync(cancellationToken);

        if (grants.Count == 0)
        {
            return GrantResolution.Denied(
                permissionKey,
                PermissionDecisionCode.PermissionRequired);
        }

        return GrantResolution.Allowed(permissionKey, grants);
    }

    private static string NormalizePermissionKey(
        string resource,
        string action) =>
        $"{resource.Trim().ToLowerInvariant()}.{action.Trim().ToLowerInvariant()}";

    private static int ScopePriority(PermissionScopeType scope) =>
        scope switch
        {
            PermissionScopeType.Tenant => 0,
            PermissionScopeType.Branch => 1,
            PermissionScopeType.Own => 2,
            _ => 99
        };

    private static bool ScopeMatches(
        Guid userId,
        PermissionScopeType scope,
        Guid? scopeReferenceId,
        PermissionScopeContext context)
    {
        return scope switch
        {
            PermissionScopeType.Tenant => true,

            PermissionScopeType.Branch =>
                context.BranchAccessValidated &&
                context.BranchId.HasValue &&
                (
                    !scopeReferenceId.HasValue ||
                    scopeReferenceId == context.BranchId
                ),

            PermissionScopeType.Own =>
                context.OwnerUserId.HasValue &&
                context.OwnerUserId == userId,

            PermissionScopeType.Child => false,

            _ => false
        };
    }

    private sealed record PermissionGrant(
        PermissionScopeType ScopeType,
        Guid? ScopeReferenceId);

    private sealed record GrantResolution(
        string PermissionKey,
        PermissionDecision? Denial,
        IReadOnlyList<PermissionGrant> Grants)
    {
        public static GrantResolution Denied(
            string key,
            PermissionDecisionCode code) =>
            new(
                key,
                PermissionDecision.Deny(key, code),
                []);

        public static GrantResolution Allowed(
            string key,
            IReadOnlyList<PermissionGrant> grants) =>
            new(key, null, grants);
    }
}
