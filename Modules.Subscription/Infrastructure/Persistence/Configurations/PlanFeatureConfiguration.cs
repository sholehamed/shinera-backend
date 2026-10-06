using Modules.System.Subscription.Domain.Entities;

namespace Modules.System.Subscription.Infrastructure.Persistence.Configurations;

internal sealed class PlanFeatureConfiguration
    : AuditEntityConfiguration<PlanFeature>
{
    public override void Configure(EntityTypeBuilder<PlanFeature> builder)
    {
        builder.ToTable("PlanFeatures");

        builder.HasIndex(x => new { x.PlanId, x.FeatureId })
            .IsUnique();

        builder.HasOne(x => x.Plan)
            .WithMany(x => x.Features)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Feature)
            .WithMany(x => x.Plans)
            .HasForeignKey(x => x.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        base.Configure(builder);
    }
}
