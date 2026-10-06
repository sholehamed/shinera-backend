using Application.SharedKernel.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Modules.System.Subscription.Application.Entitlements;
using Modules.System.Subscription.Domain.Entities;
using Modules.System.Subscription.Infrastructure.Persistence.Contexts;
using SubscriptionEntity = Modules.System.Subscription.Domain.Entities.Subscription;

namespace Application.Tests.Subscription;

public sealed class EntitlementServiceTests
{
    [Fact]
    public async Task ActiveSubscription_WithEnabledBooleanFeature_AllowsFeature()
    {
        var tenantId = Guid.NewGuid();
        await using var fixture = await CreateFixtureAsync(tenantId);

        await fixture.SeedAsync(
            tenantId,
            planIsActive: true,
            subscriptionStatus: SubscriptionStatus.Active,
            features:
            [
                new FeatureSeed(
                    FeatureKeys.AdvancedReports,
                    FeatureKind.Boolean,
                    IsEnabled: true)
            ]);

        var decision = await fixture.Service.HasFeatureAsync(
            FeatureKeys.AdvancedReports);

        Assert.True(decision.IsAllowed);
        Assert.Null(decision.ErrorCode);
    }

    [Fact]
    public async Task ActiveSubscription_WithoutFeature_DefaultDenies()
    {
        var tenantId = Guid.NewGuid();
        await using var fixture = await CreateFixtureAsync(tenantId);

        await fixture.SeedAsync(
            tenantId,
            planIsActive: true,
            subscriptionStatus: SubscriptionStatus.Active,
            features: []);

        var decision = await fixture.Service.HasFeatureAsync(
            FeatureKeys.PublicBooking);

        Assert.False(decision.IsAllowed);
        Assert.Equal(
            "subscription.feature_unavailable",
            decision.ErrorCode);
    }

    [Fact]
    public async Task InactiveSubscription_DeniesEntitlement()
    {
        var tenantId = Guid.NewGuid();
        await using var fixture = await CreateFixtureAsync(tenantId);

        await fixture.SeedAsync(
            tenantId,
            planIsActive: true,
            subscriptionStatus: SubscriptionStatus.Suspended,
            features:
            [
                new FeatureSeed(
                    FeatureKeys.AdvancedReports,
                    FeatureKind.Boolean,
                    IsEnabled: true)
            ]);

        var decision = await fixture.Service.HasFeatureAsync(
            FeatureKeys.AdvancedReports);

        Assert.False(decision.IsAllowed);
        Assert.Equal(
            "subscription.inactive",
            decision.ErrorCode);
    }

    [Fact]
    public async Task MissingSubscription_FailsSafe()
    {
        var tenantId = Guid.NewGuid();
        await using var fixture = await CreateFixtureAsync(tenantId);

        var decision = await fixture.Service.HasFeatureAsync(
            FeatureKeys.AdvancedReports);

        Assert.False(decision.IsAllowed);
        Assert.Equal(
            "subscription.required",
            decision.ErrorCode);
    }

    [Fact]
    public async Task ExistingSubscriber_IsNotRevokedOnlyBecausePlanIsNoLongerSellable()
    {
        var tenantId = Guid.NewGuid();
        await using var fixture = await CreateFixtureAsync(tenantId);

        await fixture.SeedAsync(
            tenantId,
            planIsActive: false,
            subscriptionStatus: SubscriptionStatus.Active,
            features:
            [
                new FeatureSeed(
                    FeatureKeys.AdvancedReports,
                    FeatureKind.Boolean,
                    IsEnabled: true)
            ]);

        var decision = await fixture.Service.HasFeatureAsync(
            FeatureKeys.AdvancedReports);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task InactiveFeature_DoesNotGrantCapability()
    {
        var tenantId = Guid.NewGuid();
        await using var fixture = await CreateFixtureAsync(tenantId);

        await fixture.SeedAsync(
            tenantId,
            planIsActive: true,
            subscriptionStatus: SubscriptionStatus.Active,
            features:
            [
                new FeatureSeed(
                    FeatureKeys.AdvancedReports,
                    FeatureKind.Boolean,
                    IsEnabled: true,
                    FeatureIsActive: false)
            ]);

        var decision = await fixture.Service.HasFeatureAsync(
            FeatureKeys.AdvancedReports);

        Assert.False(decision.IsAllowed);
        Assert.Equal(
            "subscription.feature_unavailable",
            decision.ErrorCode);
    }

    [Fact]
    public async Task Limit_AllowsBelowBoundary_AndDeniesAtBoundary()
    {
        var tenantId = Guid.NewGuid();
        await using var fixture = await CreateFixtureAsync(tenantId);

        await fixture.SeedAsync(
            tenantId,
            planIsActive: true,
            subscriptionStatus: SubscriptionStatus.Active,
            features:
            [
                new FeatureSeed(
                    LimitKeys.MaxBranches,
                    FeatureKind.Limit,
                    LimitValue: 1)
            ]);

        var below = await fixture.Service.CheckLimitAsync(
            LimitKeys.MaxBranches,
            currentUsage: 0);

        var atLimit = await fixture.Service.CheckLimitAsync(
            LimitKeys.MaxBranches,
            currentUsage: 1);

        var overLimit = await fixture.Service.CheckLimitAsync(
            LimitKeys.MaxBranches,
            currentUsage: 2);

        Assert.True(below.IsAllowed);
        Assert.Equal(1, below.Limit);

        Assert.False(atLimit.IsAllowed);
        Assert.Equal(
            "subscription.limit_reached",
            atLimit.ErrorCode);

        Assert.False(overLimit.IsAllowed);
        Assert.Equal(
            "subscription.over_limit",
            overLimit.ErrorCode);
    }

    [Fact]
    public async Task MissingLimit_DeniesWithFeatureUnavailable()
    {
        var tenantId = Guid.NewGuid();
        await using var fixture = await CreateFixtureAsync(tenantId);

        await fixture.SeedAsync(
            tenantId,
            planIsActive: true,
            subscriptionStatus: SubscriptionStatus.Active,
            features: []);

        var decision = await fixture.Service.CheckLimitAsync(
            LimitKeys.MaxStaff,
            currentUsage: 0);

        Assert.False(decision.IsAllowed);
        Assert.Equal(
            "subscription.feature_unavailable",
            decision.ErrorCode);
    }

    [Fact]
    public async Task TenantAEntitlement_DoesNotLeakIntoTenantB()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using var fixtureA = await CreateFixtureAsync(tenantA);
        await fixtureA.SeedAsync(
            tenantA,
            planIsActive: true,
            subscriptionStatus: SubscriptionStatus.Active,
            features:
            [
                new FeatureSeed(
                    FeatureKeys.AdvancedReports,
                    FeatureKind.Boolean,
                    IsEnabled: true)
            ]);

        var connection = fixtureA.Connection;
        await using var fixtureB = CreateFixtureOnExistingConnection(
            connection,
            tenantB,
            ownsConnection: false);

        var a = await fixtureA.Service.HasFeatureAsync(
            FeatureKeys.AdvancedReports);

        var b = await fixtureB.Service.HasFeatureAsync(
            FeatureKeys.AdvancedReports);

        Assert.True(a.IsAllowed);
        Assert.False(b.IsAllowed);
        Assert.Equal("subscription.required", b.ErrorCode);
    }

    [Fact]
    public async Task Resolver_IsCachedWithinServiceScope()
    {
        var tenantId = Guid.NewGuid();
        await using var fixture = await CreateFixtureAsync(tenantId);

        await fixture.SeedAsync(
            tenantId,
            planIsActive: true,
            subscriptionStatus: SubscriptionStatus.Active,
            features: []);

        var first = await fixture.Service.HasFeatureAsync(
            FeatureKeys.PublicBooking);

        using (fixture.CurrentTenant.DisableFilter())
        {
            var feature = new Feature(
                FeatureKeys.PublicBooking,
                "Public Booking",
                FeatureKind.Boolean);

            fixture.Db.Features.Add(feature);

            var planId = await fixture.Db.Plans
                .Select(x => x.Id)
                .SingleAsync();

            fixture.Db.PlanFeatures.Add(
                new PlanFeature(
                    planId,
                    feature.Id,
                    isEnabled: true));

            await fixture.Db.SaveChangesAsync();
        }

        var sameScope = await fixture.Service.HasFeatureAsync(
            FeatureKeys.PublicBooking);

        var freshService = new EntitlementService(
            fixture.Db,
            fixture.CurrentTenant,
            global::System.TimeProvider.System);

        var freshScope = await freshService.HasFeatureAsync(
            FeatureKeys.PublicBooking);

        Assert.False(first.IsAllowed);
        Assert.False(sameScope.IsAllowed);
        Assert.True(freshScope.IsAllowed);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Pending)]
    [InlineData(SubscriptionStatus.Suspended)]
    [InlineData(SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Expired)]
    public void Subscription_IsEntitled_RequiresActiveStatus(
        SubscriptionStatus status)
    {
        var now = DateTimeOffset.UtcNow;

        var subscription = new SubscriptionEntity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddDays(-1),
            now.AddDays(1),
            status);

        Assert.False(subscription.IsEntitled(now));
    }

    [Fact]
    public void Subscription_IsEntitled_UsesUtcPeriodBoundaries()
    {
        var now = new DateTimeOffset(
            2026, 10, 6, 5, 30, 0,
            TimeSpan.Zero);

        var subscription = new SubscriptionEntity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            now,
            now.AddDays(1),
            SubscriptionStatus.Active);

        Assert.True(subscription.IsEntitled(now));
        Assert.False(
            subscription.IsEntitled(now.AddDays(1)));
    }

    private static async Task<Fixture> CreateFixtureAsync(
        Guid tenantId)
    {
        var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var fixture = CreateFixtureOnExistingConnection(
            connection,
            tenantId,
            ownsConnection: true);

        await fixture.Db.Database.EnsureCreatedAsync();

        return fixture;
    }

    private static Fixture CreateFixtureOnExistingConnection(
        SqliteConnection connection,
        Guid tenantId,
        bool ownsConnection)
    {
        var currentTenant = new TestCurrentTenant(tenantId);

        var options =
            new DbContextOptionsBuilder<SubscriptionDbContext>()
                .UseSqlite(connection)
                .Options;

        var db = new TestSubscriptionDbContext(
            options,
            currentTenant);

        var service = new EntitlementService(
            db,
            currentTenant,
            global::System.TimeProvider.System);

        return new Fixture(
            connection,
            db,
            currentTenant,
            service,
            ownsConnection);
    }

    private sealed class TestSubscriptionDbContext(
        DbContextOptions<SubscriptionDbContext> options,
        TestCurrentTenant currentTenant)
        : SubscriptionDbContext(options, currentTenant)
    {
        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var property =
                    entityType.FindProperty("RowVersion");

                if (property is not null)
                {
                    property.ValueGenerated =
                        ValueGenerated.Never;
                }
            }
        }
    }

    private sealed class TestCurrentTenant(Guid tenantId)
        : ICurrentTenant
    {
        private int _disableDepth;

        public Guid? TenantId { get; } = tenantId;

        public IReadOnlyCollection<Guid> WritableTenantIds =>
            [tenantId];

        public bool IsFilterDisabled =>
            _disableDepth > 0;

        public IDisposable DisableFilter()
        {
            _disableDepth++;
            return new Scope(this);
        }

        private sealed class Scope(
            TestCurrentTenant currentTenant)
            : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                currentTenant._disableDepth--;
                _disposed = true;
            }
        }
    }

    private sealed record FeatureSeed(
        string Key,
        FeatureKind Kind,
        bool IsEnabled = false,
        int? LimitValue = null,
        bool FeatureIsActive = true);

    private sealed class Fixture(
        SqliteConnection connection,
        TestSubscriptionDbContext db,
        TestCurrentTenant currentTenant,
        EntitlementService service,
        bool ownsConnection)
        : IAsyncDisposable
    {
        public SqliteConnection Connection { get; } =
            connection;

        public TestSubscriptionDbContext Db { get; } =
            db;

        public TestCurrentTenant CurrentTenant { get; } =
            currentTenant;

        public EntitlementService Service { get; } =
            service;

        public async Task SeedAsync(
            Guid tenantId,
            bool planIsActive,
            SubscriptionStatus subscriptionStatus,
            IReadOnlyList<FeatureSeed> features)
        {
            using (CurrentTenant.DisableFilter())
            {
                var plan = new Plan(
                    $"plan-{Guid.NewGuid():N}",
                    "Plan",
                    isActive: planIsActive);

                Db.Plans.Add(plan);

                foreach (var seed in features)
                {
                    var feature = new Feature(
                        seed.Key,
                        seed.Key,
                        seed.Kind,
                        isActive: seed.FeatureIsActive);

                    Db.Features.Add(feature);

                    Db.PlanFeatures.Add(
                        new PlanFeature(
                            plan.Id,
                            feature.Id,
                            seed.IsEnabled,
                            seed.LimitValue));
                }

                Db.Subscriptions.Add(
                    new SubscriptionEntity(
                        tenantId,
                        plan.Id,
                        DateTimeOffset.UtcNow.AddDays(-1),
                        DateTimeOffset.UtcNow.AddDays(30),
                        subscriptionStatus));

                await Db.SaveChangesAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();

            if (ownsConnection)
                await Connection.DisposeAsync();
        }
    }
}
