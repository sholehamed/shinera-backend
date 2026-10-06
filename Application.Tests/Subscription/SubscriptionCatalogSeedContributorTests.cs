using Application.SharedKernel.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Modules.System.Subscription.Infrastructure.Persistence;
using Modules.System.Subscription.Infrastructure.Persistence.Contexts;

namespace Application.Tests.Subscription;

public sealed class SubscriptionCatalogSeedContributorTests
{
    [Fact]
    public async Task Seed_CreatesBasePlans_AndPreservesDeactivation()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<SubscriptionDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var db =
            new TestSubscriptionDbContext(
                options,
                new TestCurrentTenant());

        await db.Database.EnsureCreatedAsync();

        var seeder =
            new SubscriptionCatalogSeedContributor(db);

        await seeder.SeedAsync();

        var plans = await db.Plans
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();

        Assert.Equal(
            ["solo", "solo-pro", "salon", "salon-pro"],
            plans.Select(x => x.Key).ToArray());

        var solo = plans.Single(x => x.Key == "solo");
        solo.IsActive = false;
        await db.SaveChangesAsync();

        await seeder.SeedAsync();

        db.ChangeTracker.Clear();

        var reloadedSolo = await db.Plans
            .AsNoTracking()
            .SingleAsync(x => x.Key == "solo");

        Assert.False(reloadedSolo.IsActive);
    }

    private sealed class TestSubscriptionDbContext(
        DbContextOptions<SubscriptionDbContext> options,
        ICurrentTenant currentTenant)
        : SubscriptionDbContext(options, currentTenant)
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

    private sealed class TestCurrentTenant
        : ICurrentTenant
    {
        public Guid? UserId => null;
        public Guid? TenantId => null;
        public Guid? BranchId => null;
        public IReadOnlyCollection<Guid> WritableTenantIds => [];
        public IReadOnlyCollection<Guid> WritableBranchIds => [];
        public bool IsFilterDisabled => true;
    }
}
