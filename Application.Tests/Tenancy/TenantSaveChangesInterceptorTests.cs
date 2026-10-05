using Application.SharedKernel.Exceptions;
using Domain.SharedKernel.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Infrastructure.Persistence.Interceptors;

namespace Application.Tests.Tenancy;

public sealed class TenantSaveChangesInterceptorTests
{
    [Fact]
    public async Task AddedEntity_WithoutTenantId_IsStampedFromActiveTenant()
    {
        var tenantId = Guid.NewGuid();
        await using var fixture = await CreateFixtureAsync(tenantId, [tenantId]);

        var entity = new TenantOwnedTestEntity { Name = "service" };
        fixture.Db.Entities.Add(entity);

        await fixture.Db.SaveChangesAsync();

        Assert.Equal(tenantId, entity.TenantId);
    }

    [Fact]
    public void AddedEntity_ForAnotherTenant_IsRejected()
    {
        var activeTenantId = Guid.NewGuid();
        var foreignTenantId = Guid.NewGuid();

        using var fixture = CreateFixture(activeTenantId, [activeTenantId]);

        fixture.Db.Entities.Add(new TenantOwnedTestEntity
        {
            TenantId = foreignTenantId,
            Name = "foreign"
        });

        var exception = Assert.Throws<TenantAccessException>(
            () => fixture.Db.SaveChanges());

        Assert.Equal("tenant.cross_tenant_write", exception.Code);
    }

    [Fact]
    public async Task ModifiedEntity_FromAnotherTenant_IsRejected()
    {
        var activeTenantId = Guid.NewGuid();
        var foreignTenantId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(activeTenantId, [activeTenantId]);

        var entity = new TenantOwnedTestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = foreignTenantId,
            Name = "foreign"
        };

        fixture.Db.Attach(entity);
        entity.Name = "changed";
        fixture.Db.Entry(entity).Property(x => x.Name).IsModified = true;

        var exception = await Assert.ThrowsAsync<TenantAccessException>(
            () => fixture.Db.SaveChangesAsync());

        Assert.Equal("tenant.cross_tenant_write", exception.Code);
    }

    [Fact]
    public async Task TenantReassignment_IsRejected()
    {
        var activeTenantId = Guid.NewGuid();
        var anotherTenantId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(activeTenantId, [activeTenantId]);

        var entity = new TenantOwnedTestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = activeTenantId,
            Name = "owned"
        };

        fixture.Db.Attach(entity);
        entity.TenantId = anotherTenantId;
        fixture.Db.Entry(entity).Property(x => x.TenantId).IsModified = true;

        var exception = await Assert.ThrowsAsync<TenantAccessException>(
            () => fixture.Db.SaveChangesAsync());

        Assert.Equal("tenant.reassignment_forbidden", exception.Code);
    }

    [Fact]
    public async Task ActiveTenantWithoutWriteAccess_IsRejected()
    {
        var activeTenantId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(activeTenantId, []);

        fixture.Db.Entities.Add(new TenantOwnedTestEntity { Name = "denied" });

        var exception = await Assert.ThrowsAsync<TenantAccessException>(
            () => fixture.Db.SaveChangesAsync());

        Assert.Equal("tenant.write_denied", exception.Code);
    }

    private static Fixture CreateFixture(
        Guid activeTenantId,
        Guid[] writableTenantIds)
    {
        return CreateFixtureAsync(activeTenantId, writableTenantIds)
            .GetAwaiter()
            .GetResult();
    }

    private static async Task<Fixture> CreateFixtureAsync(
        Guid activeTenantId,
        Guid[] writableTenantIds)
    {
        var tenantContext = new TenantContext();
        tenantContext.Initialize(
            Guid.NewGuid(),
            activeTenantId,
            false,
            [activeTenantId],
            writableTenantIds,
            activeTenantId);

        var interceptor = new TenantSaveChangesInterceptor(tenantContext);
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TenantTestDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;

        var db = new TenantTestDbContext(options);
        await db.Database.EnsureCreatedAsync();

        return new Fixture(connection, db);
    }

    private sealed class TenantTestDbContext(
        DbContextOptions<TenantTestDbContext> options) : DbContext(options)
    {
        public DbSet<TenantOwnedTestEntity> Entities => Set<TenantOwnedTestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TenantOwnedTestEntity>()
                .HasKey(x => x.Id);
        }
    }

    private sealed class TenantOwnedTestEntity : IMustHaveTenant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class Fixture(
        SqliteConnection connection,
        TenantTestDbContext db) : IDisposable, IAsyncDisposable
    {
        public TenantTestDbContext Db { get; } = db;

        public void Dispose()
        {
            Db.Dispose();
            connection.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
