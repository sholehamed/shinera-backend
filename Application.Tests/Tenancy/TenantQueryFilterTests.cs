using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;

namespace Application.Tests.Tenancy;

public sealed class TenantQueryFilterTests
{
    [Fact]
    public async Task ActiveTenant_CannotReadAnotherTenantRoles()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var tenantContext = CreateTenantContext(tenantA, [tenantA, tenantB]);
        await using var fixture = await CreateFixtureAsync(tenantContext);

        fixture.Db.Roles.AddRange(
            new Role(tenantA, "Owner A", null),
            new Role(tenantB, "Owner B", null));

        await fixture.Db.SaveChangesAsync();

        var roles = await fixture.Db.Roles
            .AsNoTracking()
            .Select(x => x.Name)
            .ToListAsync();

        Assert.Single(roles);
        Assert.Equal("Owner A", roles[0]);
    }

    [Fact]
    public async Task FilterBypass_IsExplicitAndScoped()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var tenantContext = CreateTenantContext(tenantA, [tenantA, tenantB]);
        await using var fixture = await CreateFixtureAsync(tenantContext);

        fixture.Db.Roles.AddRange(
            new Role(tenantA, "Owner A", null),
            new Role(tenantB, "Owner B", null));

        await fixture.Db.SaveChangesAsync();

        int bypassedCount;
        using (tenantContext.DisableFilter())
        {
            bypassedCount = await fixture.Db.Roles.CountAsync();
        }

        var filteredCount = await fixture.Db.Roles.CountAsync();

        Assert.Equal(2, bypassedCount);
        Assert.Equal(1, filteredCount);
    }

    private static TenantContext CreateTenantContext(
        Guid activeTenantId,
        Guid[] readableTenantIds)
    {
        var context = new TenantContext();
        context.Initialize(
            Guid.NewGuid(),
            false,
            readableTenantIds,
            [activeTenantId],
            activeTenantId);

        return context;
    }

    private static async Task<Fixture> CreateFixtureAsync(
        TenantContext tenantContext)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new TenantQueryTestDbContext(options, tenantContext);
        await db.Database.EnsureCreatedAsync();

        return new Fixture(connection, db);
    }

    private sealed class TenantQueryTestDbContext(
        DbContextOptions<IdentityDbContext> options,
        TenantContext tenantContext)
        : IdentityDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // SQL Server generates rowversion values. SQLite does not.
            // These tests exercise tenant filtering, not SQL Server rowversion semantics.
            builder.Entity<Role>()
                .Property(x => x.RowVersion)
                .ValueGeneratedNever();
        }
    }

    private sealed class Fixture(
        SqliteConnection connection,
        IdentityDbContext db) : IAsyncDisposable
    {
        public IdentityDbContext Db { get; } = db;

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
