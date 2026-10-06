using Modules.System.Subscription.Domain.Entities;

namespace Modules.System.Subscription.Infrastructure.Persistence.Configurations;

internal sealed class FeatureConfiguration : AuditEntityConfiguration<Feature>
{
    public override void Configure(EntityTypeBuilder<Feature> builder)
    {
        builder.ToTable("Features");

        builder.Property(x => x.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.Key)
            .IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.Kind)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        base.Configure(builder);
    }
}
