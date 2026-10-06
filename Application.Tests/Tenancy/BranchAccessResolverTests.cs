using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;

namespace Application.Tests.Tenancy;

public sealed class BranchAccessResolverTests
{
    [Fact]
    public async Task TenantMembership_WithoutBranchMembership_DoesNotGrantBranchAccess()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var fixture =
            await CreateFixtureAsync(tenantId);

        await fixture.SeedTenantAndUserAsync(
            tenantId,
            userId);

        fixture.Db.TenantMemberships.Add(
            new TenantMembership(
                tenantId,
                userId));

        fixture.Db.Branches.Add(
            new Branch(
                tenantId,
                "Main",
                isMain: true));

        await fixture.Db.SaveChangesAsync();

        var resolver =
            new BranchAccessResolver(fixture.Db);

        var access = await resolver.ResolveAsync(
            userId,
            tenantId,
            CancellationToken.None);

        Assert.Empty(access.Readable);
        Assert.Empty(access.Writable);
    }

    [Fact]
    public async Task Resolver_ReturnsOnlyActiveExplicitMemberships_ForRequestedTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var fixture =
            await CreateFixtureAsync(tenantA);

        await fixture.SeedTenantAndUserAsync(
            tenantA,
            userId,
            tenantB);

        var activeA = new Branch(
            tenantA,
            "A Main",
            isMain: true);

        var inactiveA = new Branch(
            tenantA,
            "A Inactive",
            isActive: false);

        var activeB = new Branch(
            tenantB,
            "B Main",
            isMain: true);

        fixture.Db.Branches.AddRange(
            activeA,
            inactiveA,
            activeB);

        fixture.Db.BranchMemberships.AddRange(
            new BranchMembership(
                tenantA,
                activeA.Id,
                userId),
            new BranchMembership(
                tenantA,
                inactiveA.Id,
                userId),
            new BranchMembership(
                tenantB,
                activeB.Id,
                userId));

        await fixture.Db.SaveChangesAsync();

        var resolver =
            new BranchAccessResolver(fixture.Db);

        var access = await resolver.ResolveAsync(
            userId,
            tenantA,
            CancellationToken.None);

        Assert.Equal(
            [activeA.Id],
            access.Readable);
        Assert.Equal(
            [activeA.Id],
            access.Writable);
    }

    private static async Task<Fixture> CreateFixtureAsync(
        Guid activeTenantId)
    {
        var tenantContext = new TenantContext();

        tenantContext.Initialize(
            Guid.NewGuid(),
            false,
            [activeTenantId],
            [activeTenantId],
            activeTenantId);

        var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlite(connection)
                .Options;

        var db =
            new BranchAccessTestDbContext(
                options,
                tenantContext);

        await db.Database.EnsureCreatedAsync();

        return new Fixture(
            connection,
            db,
            tenantContext);
    }

    private sealed class BranchAccessTestDbContext(
        DbContextOptions<IdentityDbContext> options,
        TenantContext tenantContext)
        : IdentityDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var entityType in builder.Model.GetEntityTypes())
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

    private sealed class Fixture(
        SqliteConnection connection,
        IdentityDbContext db,
        TenantContext tenantContext)
        : IAsyncDisposable
    {
        public IdentityDbContext Db { get; } = db;
        public TenantContext TenantContext { get; } =
            tenantContext;

        public async Task SeedTenantAndUserAsync(
            Guid tenantId,
            Guid userId,
            Guid? secondTenantId = null)
        {
            using (TenantContext.DisableFilter())
            {
                Db.Tenants.Add(
                    CreateTenant(
                        tenantId,
                        "Tenant A"));

                if (secondTenantId.HasValue)
                {
                    Db.Tenants.Add(
                        CreateTenant(
                            secondTenantId.Value,
                            "Tenant B"));
                }

                Db.Users.Add(
                    new User(
                        userId,
                        $"user-{userId:N}",
                        $"{userId:N}@example.test",
                        "Test",
                        "User")
                    {
                        PasswordHash =
                            "test-password-hash",
                        SecurityStamp =
                            Guid.NewGuid().ToString("N"),
                        ConcurrencyStamp =
                            Guid.NewGuid().ToString("N")
                    });

                await Db.SaveChangesAsync();
            }
        }

        private static Tenant CreateTenant(
            Guid id,
            string name) =>
            new(
                id,
                null,
                name,
                $"tenant-{id:N}",
                $"{id:N}.example.test");

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
