using Application.SharedKernel.Exceptions;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Application.Features.Roles.Commands;

public sealed class UpdateRolePermissionsCommand : ICommand
{
    public Guid? RoleId { get; set; }
    public List<Guid> PermissionIds { get; set; } = [];
}

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

        var permissionIds = request.PermissionIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var roleExists = await context.Roles
            .AsNoTracking()
            .AnyAsync(
                role => role.Id == roleId && role.IsActive,
                cancellationToken);

        if (!roleExists)
            throw new ServiceExeption("Role not found.");

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

        var assignments = validPermissionIds.Select(permissionId =>
            new PermissionAssignment(
                tenantId,
                permissionId,
                PermissionSubjectType.Role,
                roleId,
                PermissionScopeType.Tenant));

        await context.PermissionAssignments.AddRangeAsync(
            assignments,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}
