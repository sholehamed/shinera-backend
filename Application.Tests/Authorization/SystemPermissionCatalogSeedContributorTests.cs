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
            var servicePermissionCodes =
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
                        code.StartsWith("services."))
                    .OrderBy(code => code)
                    .ToListAsync();

            Assert.Equal(
                new[]
                {
                    "services.create",
                    "services.delete",
                    "services.update",
                    "services.view"
                },
                servicePermissionCodes);
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
