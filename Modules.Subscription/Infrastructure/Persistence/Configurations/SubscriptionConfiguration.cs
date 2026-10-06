using Modules.System.Subscription.Domain.Entities;
using SubscriptionEntity = Modules.System.Subscription.Domain.Entities.Subscription;

namespace Modules.System.Subscription.Infrastructure.Persistence.Configurations;

internal sealed class SubscriptionConfiguration
    : AuditEntityConfiguration<SubscriptionEntity>
{
    public override void Configure(EntityTypeBuilder<SubscriptionEntity> builder)
    {
        builder.ToTable("Subscriptions");

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.StartedAtUtc)
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.Property(x => x.EndsAtUtc)
            .HasColumnType("datetimeoffset");

        builder.Property(x => x.CancelledAtUtc)
            .HasColumnType("datetimeoffset");

        builder.Property(x => x.ExternalReference)
            .HasMaxLength(300);

        builder.HasIndex(x => new
            {
                x.TenantId,
                x.StartedAtUtc
            });

        builder.HasIndex(x => x.TenantId)
            .IsUnique()
            .HasFilter("[Status] = 2");

        builder.HasOne(x => x.Plan)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        base.Configure(builder);
    }
}
