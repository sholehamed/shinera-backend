using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Identity.Infrastructure.Persistence;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;

namespace Application.Tests.Authorization;

public sealed class SystemPermissionCatalogSeedContributorTests
{
    [Fact]
    public async Task Seed_BackfillsWorkspaceOwnerWithNewServicePermissions_Idempotently()
    {
        var tenantId = Guid.NewGuid();

        var tenantContext = new TenantContext();
        tenantContext.Initialize(
            Guid.NewGuid(),
            false,
            [tenantId],
            [tenantId],
            tenantId);

        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var db =
            new TestIdentityDbContext(
                options,
                tenantContext);

        await db.Database.EnsureCreatedAsync();

        var ownerRole = new Role(
            tenantId,
            "Owner",
            "Workspace owner");

        using (tenantContext.DisableFilter())
        {
            db.Tenants.Add(
                new Tenant(tenantId)
                {
                    Name = "Tenant",
                    Slug = $"tenant-{tenantId:N}",
                    IsActive = true
                });

            db.Roles.Add(ownerRole);
            await db.SaveChangesAsync();

            var systemModule = new Modules.System.Identity.Domain.Entities.Module(
                "system",
                "System",
                null,
                0);

            db.Modules.Add(systemModule);

            var baselineKeys =
                SystemPermissionCatalog.WorkspaceOwnerBaselinePermissionKeys;

            foreach (var resourceGroup in
                     baselineKeys.GroupBy(
                         key => key.Split('.')[0]))
            {
                var resource = new Resource(
                    systemModule.Id,
                    resourceGroup.Key,
                    resourceGroup.Key,
                    null,
                    0);

                db.Resources.Add(resource);

                foreach (var key in resourceGroup)
                {
                    var permission = new Permission(
                        key,
                        resource,
                        key,
                        null);

                    db.Permissions.Add(permission);
                    db.PermissionAssignments.Add(
                        new PermissionAssignment(
                            tenantId,
                            permission.Id,
                            PermissionSubjectType.Role,
                            ownerRole.Id,
                            PermissionScopeType.Tenant));
                }
            }

            await db.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddSingleton<IdentityDbContext>(db);
        services.AddSingleton<ITenantContext>(tenantContext);
        services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder().Build());

        await using var provider =
            services.BuildServiceProvider();

        var seeder =
            new SystemPermissionCatalogSeedContributor();

        await seeder.SeedAsync(provider);
        await seeder.SeedAsync(provider);

        using (tenantContext.DisableFilter())
        {
            var workspacePermissionCodes =
                await db.PermissionAssignments
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenantId &&
                        x.SubjectType ==
                            PermissionSubjectType.Role &&
                        x.SubjectId == ownerRole.Id &&
                        x.ScopeType ==
                            PermissionScopeType.Tenant &&
                        x.IsActive)
                    .Join(
                        db.Permissions.AsNoTracking(),
                        assignment =>
                            assignment.PermissionId,
                        permission =>
                            permission.Id,
                        (assignment, permission) =>
                            permission.Code)
                    .Where(code =>
                        code.StartsWith("services.") ||
                        code.StartsWith("staff.") ||
                        code.StartsWith("customers.") ||
                        code.StartsWith("appointments."))
                    .OrderBy(code => code)
                    .ToListAsync();

            Assert.Equal(
                new[]
                {
                    "appointments.create",
                    "appointments.view",
                    "customers.add_note",
                    "customers.create",
                    "customers.view",
                    "services.create",
                    "services.delete",
                    "services.update",
                    "services.view",
                    "staff.assign_branches",
                    "staff.assign_services",
                    "staff.create",
                    "staff.update",
                    "staff.update_schedule",
                    "staff.view",
                    "staff.view_schedule"
                },
                workspacePermissionCodes);
        }
    }

    [Fact]
    public async Task Seed_DoesNotElevateArbitraryRoleNamedOwner()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = new TenantContext();
        tenantContext.Initialize(
            Guid.NewGuid(),
            false,
            [tenantId],
            [tenantId],
            tenantId);

        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var db =
            new TestIdentityDbContext(
                options,
                tenantContext);

        await db.Database.EnsureCreatedAsync();

        var customOwner = new Role(
            tenantId,
            "Owner",
            "Custom role with colliding name");

        using (tenantContext.DisableFilter())
        {
            db.Tenants.Add(
                new Tenant(tenantId)
                {
                    Name = "Tenant",
                    Slug = $"tenant-{tenantId:N}",
                    IsActive = true
                });

            db.Roles.Add(customOwner);
            await db.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddSingleton<IdentityDbContext>(db);
        services.AddSingleton<ITenantContext>(tenantContext);
        services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder().Build());

        await using var provider =
            services.BuildServiceProvider();

        var seeder =
            new SystemPermissionCatalogSeedContributor();

        await seeder.SeedAsync(provider);

        using (tenantContext.DisableFilter())
        {
            var assignedServicePermissions =
                await db.PermissionAssignments
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenantId &&
                        x.SubjectType ==
                            PermissionSubjectType.Role &&
                        x.SubjectId == customOwner.Id)
                    .Join(
                        db.Permissions.AsNoTracking(),
                        assignment => assignment.PermissionId,
                        permission => permission.Id,
                        (assignment, permission) =>
                            permission.Code)
                    .CountAsync(code =>
                        code.StartsWith("services.") ||
                        code.StartsWith("staff.") ||
                        code.StartsWith("customers.") ||
                        code.StartsWith("appointments."));

            Assert.Equal(
                0,
                assignedServicePermissions);
        }
    }

    private sealed class TestIdentityDbContext(
        DbContextOptions<IdentityDbContext> options,
        TenantContext tenantContext)
        : IdentityDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var entityType in
                     builder.Model.GetEntityTypes())
            {
                var rowVersion =
                    entityType.FindProperty("RowVersion");

                if (rowVersion is not null)
                {
                    rowVersion.ValueGenerated =
                        ValueGenerated.Never;
                }
            }
        }
    }
}
