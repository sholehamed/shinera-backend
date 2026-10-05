using Microsoft.EntityFrameworkCore;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Application.Authorization;

public sealed class PermissionAuthorizationService(
    IIdentityDbContext dbContext,
    ITenantContext tenantContext)
    : IPermissionAuthorizationService
{
    public async Task<PermissionDecision> AuthorizeAsync(
        Guid userId,
        string resource,
        string action,
        PermissionScopeContext? resourceContext = null,
        CancellationToken cancellationToken = default)
    {
        var permissionKey = NormalizePermissionKey(resource, action);

        var tenantId = tenantContext.ActiveTenantId;
        if (!tenantId.HasValue)
        {
            return PermissionDecision.Deny(
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
            return PermissionDecision.Deny(
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
            return PermissionDecision.Deny(
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

        var assignments = await dbContext.PermissionAssignments
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
            .Select(assignment => new
            {
                assignment.ScopeType,
                assignment.ScopeReferenceId
            })
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
        {
            return PermissionDecision.Deny(
                permissionKey,
                PermissionDecisionCode.PermissionRequired);
        }

        foreach (var assignment in assignments
                     .OrderBy(assignment => ScopePriority(assignment.ScopeType)))
        {
            if (ScopeMatches(
                    userId,
                    assignment.ScopeType,
                    assignment.ScopeReferenceId,
                    resourceContext))
            {
                return PermissionDecision.Allow(
                    permissionKey,
                    assignment.ScopeType);
            }
        }

        return PermissionDecision.Deny(
            permissionKey,
            PermissionDecisionCode.ScopeDenied);
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

        return await dbContext.PermissionAssignments
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
            .OrderBy(assignment => assignment.Permission.Code)
            .ThenBy(assignment => assignment.ScopeType)
            .Select(assignment => new EffectivePermissionDto(
                assignment.Permission.Code,
                assignment.ScopeType.ToString(),
                assignment.ScopeReferenceId))
            .Distinct()
            .ToListAsync(cancellationToken);
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
        PermissionScopeContext? context)
    {
        return scope switch
        {
            PermissionScopeType.Tenant => true,

            PermissionScopeType.Branch =>
                context?.BranchAccessValidated == true &&
                context.BranchId.HasValue &&
                (
                    !scopeReferenceId.HasValue ||
                    scopeReferenceId == context.BranchId
                ),

            PermissionScopeType.Own =>
                context?.OwnerUserId.HasValue == true &&
                context.OwnerUserId == userId,

            // Child is intentionally reserved until a concrete hierarchy exists.
            PermissionScopeType.Child => false,

            _ => false
        };
    }
}
