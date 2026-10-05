using Application.SharedKernel.Exceptions;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Application.Features.Roles.Commands;

public sealed class UpdateRolePermissionsCommand : ICommand
{
    public Guid? RoleId { get; set; }

    // Legacy compatibility. Entries here are treated as Tenant-scoped.
    public List<Guid> PermissionIds { get; set; } = [];

    public List<RolePermissionAssignmentInput> Assignments { get; set; } = [];
}

public sealed record RolePermissionAssignmentInput(
    Guid PermissionId,
    PermissionScopeType ScopeType,
    Guid? ScopeReferenceId = null);

public sealed class UpdateRolePermissionsCommandHandler(
    IIdentityDbContext context,
    ITenantContext tenantContext)
    : ICommandHandler<UpdateRolePermissionsCommand>
{
    public async Task Handle(
        UpdateRolePermissionsCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.ActiveTenantId
            ?? throw new TenantAccessException(
                "tenant.context_missing",
                "An active tenant is required to manage role permissions.");

        var roleId = request.RoleId
            ?? throw new ServiceExeption("Role is required.");

        var roleExists = await context.Roles
            .AsNoTracking()
            .AnyAsync(
                role => role.Id == roleId && role.IsActive,
                cancellationToken);

        if (!roleExists)
            throw new ServiceExeption("Role not found.");

        var requestedAssignments =
            request.Assignments.Count > 0
                ? request.Assignments
                : request.PermissionIds
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .Select(id => new RolePermissionAssignmentInput(
                        id,
                        PermissionScopeType.Tenant))
                    .ToList();

        if (requestedAssignments.Any(x =>
                x.ScopeType == PermissionScopeType.Child))
        {
            throw new ServiceExeption(
                "Child permission scope is reserved and is not supported.");
        }

        var normalizedAssignments = requestedAssignments
            .Where(x => x.PermissionId != Guid.Empty)
            .Select(x => x with
            {
                ScopeReferenceId =
                    x.ScopeType == PermissionScopeType.Branch
                        ? x.ScopeReferenceId
                        : null
            })
            .Distinct()
            .ToList();

        var permissionIds = normalizedAssignments
            .Select(x => x.PermissionId)
            .Distinct()
            .ToList();

        var validPermissionIds = await context.Permissions
            .AsNoTracking()
            .Where(permission =>
                permissionIds.Contains(permission.Id) &&
                permission.IsActive &&
                permission.Resource != null &&
                permission.Resource.IsActive)
            .Select(permission => permission.Id)
            .ToListAsync(cancellationToken);

        if (validPermissionIds.Count != permissionIds.Count)
            throw new ServiceExeption("One or more permissions are invalid.");

        var existingAssignments = await context.PermissionAssignments
            .Where(assignment =>
                assignment.SubjectType == PermissionSubjectType.Role &&
                assignment.SubjectId == roleId)
            .ToListAsync(cancellationToken);

        context.PermissionAssignments.RemoveRange(existingAssignments);

        var assignments = normalizedAssignments.Select(item =>
            new PermissionAssignment(
                tenantId,
                item.PermissionId,
                PermissionSubjectType.Role,
                roleId,
                item.ScopeType,
                item.ScopeReferenceId));

        await context.PermissionAssignments.AddRangeAsync(
            assignments,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}
