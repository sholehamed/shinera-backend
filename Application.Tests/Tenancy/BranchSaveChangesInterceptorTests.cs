using Application.SharedKernel.Exceptions;
using Domain.SharedKernel.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Infrastructure.Persistence.Interceptors;

namespace Application.Tests.Tenancy;

public sealed class BranchSaveChangesInterceptorTests
{
    [Fact]
    public async Task AddedBranchOwnedEntity_IsStampedFromActiveWorkspace()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        await using var fixture =
            await CreateFixtureAsync(tenantId, branchId);

        var entity = new BranchOwnedProbe
        {
            Name = "appointment"
        };

        fixture.Db.Entities.Add(entity);
        await fixture.Db.SaveChangesAsync();

        Assert.Equal(tenantId, entity.TenantId);
        Assert.Equal(branchId, entity.BranchId);
    }

    [Fact]
    public async Task AddedEntity_ForAnotherBranch_IsRejected()
    {
        var tenantId = Guid.NewGuid();
        var activeBranchId = Guid.NewGuid();

        await using var fixture =
            await CreateFixtureAsync(
                tenantId,
                activeBranchId);

        fixture.Db.Entities.Add(
            new BranchOwnedProbe
            {
                BranchId = Guid.NewGuid(),
                Name = "foreign"
            });

        var exception =
            await Assert.ThrowsAsync<TenantAccessException>(
                () => fixture.Db.SaveChangesAsync());

        Assert.Equal(
            "branch.cross_branch_write",
            exception.Code);
    }

    [Fact]
    public async Task BranchOwnedWrite_WithoutActiveBranch_IsRejected()
    {
        var tenantId = Guid.NewGuid();

        await using var fixture =
            await CreateFixtureAsync(
                tenantId,
                activeBranchId: null);

        fixture.Db.Entities.Add(
            new BranchOwnedProbe
            {
                Name = "missing-context"
            });

        var exception =
            await Assert.ThrowsAsync<TenantAccessException>(
                () => fixture.Db.SaveChangesAsync());

        Assert.Equal(
            "branch.context_missing",
            exception.Code);
    }

    private static async Task<Fixture> CreateFixtureAsync(
        Guid tenantId,
        Guid? activeBranchId)
    {
        var tenantContext = new TenantContext();

        tenantContext.Initialize(
            Guid.NewGuid(),
            false,
            [tenantId],
            [tenantId],
            tenantId,
            activeBranchId.HasValue
                ? [activeBranchId.Value]
                : [],
            activeBranchId.HasValue
                ? [activeBranchId.Value]
                : [],
            activeBranchId);

        var interceptor =
            new TenantSaveChangesInterceptor(tenantContext);

        var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<BranchTestDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(interceptor)
                .Options;

        var db = new BranchTestDbContext(options);
        await db.Database.EnsureCreatedAsync();

        return new Fixture(connection, db);
    }

    private sealed class BranchTestDbContext(
        DbContextOptions<BranchTestDbContext> options)
        : DbContext(options)
    {
        public DbSet<BranchOwnedProbe> Entities =>
            Set<BranchOwnedProbe>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BranchOwnedProbe>()
                .HasKey(x => x.Id);
        }
    }

    private sealed class BranchOwnedProbe
        : IMustHaveTenant, IMustHaveBranch
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid BranchId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class Fixture(
        SqliteConnection connection,
        BranchTestDbContext db)
        : IAsyncDisposable
    {
        public BranchTestDbContext Db { get; } = db;

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
