using Modules.System.Subscription.Domain.Entities;
using Modules.System.Subscription.Infrastructure.Persistence.Contexts;

namespace Modules.System.Subscription.Infrastructure.Persistence;

public sealed class SubscriptionCatalogSeedContributor(
    SubscriptionDbContext db)
{
    private static readonly PlanDefinition[] Plans =
    [
        new(
            "solo",
            "Solo",
            "Core plan for independent beauty professionals.",
            10),
        new(
            "solo-pro",
            "Solo Pro",
            "Advanced plan for growing independent professionals.",
            20),
        new(
            "salon",
            "Salon",
            "Core plan for salon teams.",
            30),
        new(
            "salon-pro",
            "Salon Pro",
            "Advanced plan for multi-staff and multi-branch salons.",
            40)
    ];

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var definition in Plans)
        {
            var key = Plan.NormalizeKey(definition.Key);

            var plan = await db.Plans
                .SingleOrDefaultAsync(
                    x => x.Key == key,
                    cancellationToken);

            if (plan is null)
            {
                db.Plans.Add(
                    new Plan(
                        definition.Key,
                        definition.Name,
                        definition.Description,
                        definition.DisplayOrder));

                continue;
            }

            plan.Name = definition.Name;
            plan.Description = definition.Description;
            plan.DisplayOrder = definition.DisplayOrder;
            // Preserve an explicit administrative deactivation.
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private sealed record PlanDefinition(
        string Key,
        string Name,
        string Description,
        int DisplayOrder);
}
