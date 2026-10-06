using System.Reflection;
using Modules.System.Subscription.Application.Abstractions;
using Modules.System.Subscription.Domain.Entities;
using SubscriptionEntity = Modules.System.Subscription.Domain.Entities.Subscription;

namespace Modules.System.Subscription.Infrastructure.Persistence.Contexts;

public class SubscriptionDbContext(
    DbContextOptions<SubscriptionDbContext> options,
    ICurrentTenant currentTenant)
    : BaseDbContext(options), IEntitlementDbContext
{
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
    public DbSet<SubscriptionEntity> Subscriptions => Set<SubscriptionEntity>();

    private bool FilterDisabled => currentTenant.IsFilterDisabled;
    private Guid CurrentTenantId => currentTenant.TenantId ?? Guid.Empty;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly());

        base.OnModelCreating(builder);

        builder.Entity<SubscriptionEntity>()
            .HasQueryFilter(
                "tenant",
                subscription =>
                    FilterDisabled ||
                    subscription.TenantId == CurrentTenantId);
    }
}
