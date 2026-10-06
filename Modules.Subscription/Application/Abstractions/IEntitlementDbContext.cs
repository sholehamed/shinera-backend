using Modules.System.Subscription.Domain.Entities;

namespace Modules.System.Subscription.Application.Abstractions;

public interface IEntitlementDbContext : IBaseDbContext
{
    DbSet<Plan> Plans { get; }
    DbSet<Feature> Features { get; }
    DbSet<PlanFeature> PlanFeatures { get; }
    DbSet<Domain.Entities.Subscription> Subscriptions { get; }
}
