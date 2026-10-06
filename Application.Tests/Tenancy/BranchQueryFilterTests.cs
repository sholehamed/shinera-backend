using Domain.SharedKernel.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;

namespace Application.Tests.Tenancy;

public sealed class BranchQueryFilterTests
{
    [Fact]
    public async Task ActiveWorkspace_CannotReadAnotherBranch()
    {
        var tenantId = Guid.NewGuid();
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();

        var tenantContext = new TenantContext();
        tenantContext.Initialize(
            Guid.NewGuid(),
            false,
            [tenantId],
            [tenantId],
            tenantId,
            [branchA, branchB],
            [branchA, branchB],
            branchA);

        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var db =
            new BranchQueryTestDbContext(
                options,
                tenantContext);

        await db.Database.EnsureCreatedAsync();

        using (tenantContext.DisableFilter())
        {
            db.Probes.AddRange(
                new BranchScopedProbe
                {
                    TenantId = tenantId,
                    BranchId = branchA,
                    Name = "A"
                },
                new BranchScopedProbe
                {
                    TenantId = tenantId,
                    BranchId = branchB,
                    Name = "B"
                });

            await db.SaveChangesAsync();
        }

        var visible = await db.Probes
            .AsNoTracking()
            .Select(x => x.Name)
            .ToListAsync();

        Assert.Single(visible);
        Assert.Equal("A", visible[0]);
    }

    private sealed class BranchQueryTestDbContext(
        DbContextOptions<IdentityDbContext> options,
        TenantContext tenantContext)
        : IdentityDbContext(options, tenantContext)
    {
        public DbSet<BranchScopedProbe> Probes =>
            Set<BranchScopedProbe>();

        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            builder.Entity<BranchScopedProbe>()
                .HasKey(x => x.Id);

            base.OnModelCreating(builder);
        }
    }

    private sealed class BranchScopedProbe
        : IMustHaveTenant, IMustHaveBranch
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid BranchId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
