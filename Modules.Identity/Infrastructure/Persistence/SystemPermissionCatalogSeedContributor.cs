using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;

namespace Modules.System.Identity.Infrastructure.Persistence;

public sealed class SystemPermissionCatalogSeedContributor
{
    private static readonly CatalogResource[] Resources =
    [
        new(
            SystemPermissionCatalog.Tenants.Resource,
            "Tenants",
            [
                SystemPermissionCatalog.Tenants.List,
                SystemPermissionCatalog.Tenants.Create,
                SystemPermissionCatalog.Tenants.Update,
                SystemPermissionCatalog.Tenants.Delete
            ]),
        new(
            SystemPermissionCatalog.Branches.Resource,
            "Branches",
            [
                SystemPermissionCatalog.Branches.List,
                SystemPermissionCatalog.Branches.Create,
                SystemPermissionCatalog.Branches.Update,
                SystemPermissionCatalog.Branches.Disable,
                SystemPermissionCatalog.Branches.SetMain
            ]),
        new(
            SystemPermissionCatalog.BusinessProfile.Resource,
            "Business Profile",
            [
                SystemPermissionCatalog.BusinessProfile.View,
                SystemPermissionCatalog.BusinessProfile.Update
            ]),
        new(
            SystemPermissionCatalog.Modules.Resource,
            "Modules",
            [
                SystemPermissionCatalog.Modules.List,
                SystemPermissionCatalog.Modules.Create,
                SystemPermissionCatalog.Modules.Update,
                SystemPermissionCatalog.Modules.Delete
            ]),
        new(
            SystemPermissionCatalog.Resources.Resource,
            "Resources",
            [
                SystemPermissionCatalog.Resources.List,
                SystemPermissionCatalog.Resources.Create,
                SystemPermissionCatalog.Resources.Update,
                SystemPermissionCatalog.Resources.Delete
            ]),
        new(
            SystemPermissionCatalog.Users.Resource,
            "Users",
            [
                SystemPermissionCatalog.Users.List,
                SystemPermissionCatalog.Users.Create,
                SystemPermissionCatalog.Users.Update,
                SystemPermissionCatalog.Users.Delete
            ]),
        new(
            SystemPermissionCatalog.Roles.Resource,
            "Roles",
            [
                SystemPermissionCatalog.Roles.List,
                SystemPermissionCatalog.Roles.Create,
                SystemPermissionCatalog.Roles.Update,
                SystemPermissionCatalog.Roles.Delete
            ]),
        new(
            SystemPermissionCatalog.Groups.Resource,
            "Groups",
            [
                SystemPermissionCatalog.Groups.List,
                SystemPermissionCatalog.Groups.Create,
                SystemPermissionCatalog.Groups.Update,
                SystemPermissionCatalog.Groups.Delete
            ]),
        new(
            SystemPermissionCatalog.Permissions.Resource,
            "Permissions",
            [
                SystemPermissionCatalog.Permissions.List,
                SystemPermissionCatalog.Permissions.Create,
                SystemPermissionCatalog.Permissions.Update,
                SystemPermissionCatalog.Permissions.Delete
            ]),
        new(
            SystemPermissionCatalog.Menus.Resource,
            "Menus",
            [
                SystemPermissionCatalog.Menus.List,
                SystemPermissionCatalog.Menus.Create,
                SystemPermissionCatalog.Menus.Update,
                SystemPermissionCatalog.Menus.Delete
            ]),
        new(
            SystemPermissionCatalog.Dashboard.Resource,
            "Dashboard",
            [
                SystemPermissionCatalog.Dashboard.View
            ])
    ];

    public async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<IdentityDbContext>();
        var tenantContext = serviceProvider.GetRequiredService<ITenantContext>();

        var systemModule = await db.Modules
            .SingleOrDefaultAsync(module =>
                module.Code == "system");

        if (systemModule is null)
        {
            systemModule = new Module(
                "system",
                "System",
                "System administration",
                0);

            db.Modules.Add(systemModule);
            await db.SaveChangesAsync();
        }

        var permissions = new List<Permission>();

        foreach (var definition in Resources)
        {
            var resource = await db.Resources
                .SingleOrDefaultAsync(item =>
                    item.ModuleId == systemModule.Id &&
                    item.Code == definition.Code);

            if (resource is null)
            {
                resource = new Resource(
                    systemModule.Id,
                    definition.Code,
                    definition.Title,
                    null,
                    0);

                db.Resources.Add(resource);
                await db.SaveChangesAsync();
            }

            foreach (var action in definition.Actions)
            {
                var key = SystemPermissionCatalog.Key(
                    definition.Code,
                    action);

                var permission = await db.Permissions
                    .SingleOrDefaultAsync(item =>
                        item.Code == key);

                if (permission is null)
                {
                    permission = new Permission(
                        key,
                        resource,
                        key,
                        null);

                    db.Permissions.Add(permission);
                    await db.SaveChangesAsync();
                }
                else
                {
                    var changed = false;

                    if (permission.ResourceId != resource.Id)
                    {
                        permission.ResourceId = resource.Id;
                        permission.Resource = resource;
                        changed = true;
                    }

                    if (!string.Equals(
                            permission.Action,
                            action,
                            StringComparison.Ordinal))
                    {
                        permission.Action = action;
                        changed = true;
                    }

                    if (changed)
                        await db.SaveChangesAsync();
                }

                permissions.Add(permission);
            }
        }

        var configuration =
            serviceProvider.GetRequiredService<IConfiguration>();

        var rawSystemTenantId =
            configuration["Identity:SystemTenantId"];

        if (string.IsNullOrWhiteSpace(rawSystemTenantId))
        {
            // Catalog definitions are global. Tenant assignments are left
            // untouched unless a platform System Tenant is explicitly configured.
            return;
        }

        if (!Guid.TryParse(rawSystemTenantId, out var systemTenantId))
        {
            throw new InvalidOperationException(
                "Identity:SystemTenantId must be a valid Guid when configured.");
        }

        using (tenantContext.DisableFilter())
        {
            var systemTenantExists = await db.Tenants
                .AsNoTracking()
                .AnyAsync(tenant =>
                    tenant.Id == systemTenantId &&
                    tenant.IsActive);

            if (!systemTenantExists)
            {
                throw new InvalidOperationException(
                    "Identity:SystemTenantId does not reference an active Tenant.");
            }

            var superAdminRoles = await db.Roles
                .AsNoTracking()
                .Where(role =>
                    role.TenantId == systemTenantId &&
                    role.NormalizedName == "SUPERADMIN" &&
                    role.IsActive)
                .Select(role => new
                {
                    role.Id,
                    role.TenantId
                })
                .ToListAsync();

            foreach (var role in superAdminRoles)
            {
                var existingPermissionIds =
                    await db.PermissionAssignments
                        .AsNoTracking()
                        .Where(assignment =>
                            assignment.TenantId == role.TenantId &&
                            assignment.SubjectType ==
                                PermissionSubjectType.Role &&
                            assignment.SubjectId == role.Id &&
                            assignment.ScopeType ==
                                PermissionScopeType.Tenant &&
                            assignment.IsActive)
                        .Select(assignment =>
                            assignment.PermissionId)
                        .ToListAsync();

                var missing = permissions
                    .Where(permission =>
                        !existingPermissionIds.Contains(
                            permission.Id))
                    .Select(permission =>
                        new PermissionAssignment(
                            role.TenantId,
                            permission.Id,
                            PermissionSubjectType.Role,
                            role.Id,
                            PermissionScopeType.Tenant))
                    .ToList();

                if (missing.Count > 0)
                {
                    db.PermissionAssignments.AddRange(missing);
                    await db.SaveChangesAsync();
                }
            }
        }
    }

    private sealed record CatalogResource(
        string Code,
        string Title,
        string[] Actions);
}
