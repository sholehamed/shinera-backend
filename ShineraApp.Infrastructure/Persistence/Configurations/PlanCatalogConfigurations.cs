using Infrastructure.SharedKernel.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShineraApp.Domain.Entities;

namespace ShineraApp.Infrastructure.Persistence.Configurations;

public sealed class PlanConfiguration : FullAuditEntityConfiguration<Plan>
{
    public override void Configure(EntityTypeBuilder<Plan> b)
    {
        base.Configure(b);
        b.ToTable("Plans", "Catalog");
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Code).HasMaxLength(64).IsRequired();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasMany(x => x.Prices).WithOne(x => x.Plan).HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Features).WithOne(x => x.Plan).HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PlanPriceConfiguration : FullAuditEntityConfiguration<PlanPrice>
{
    public override void Configure(EntityTypeBuilder<PlanPrice> b)
    {
        base.Configure(b);
        b.ToTable("PlanPrices", "Catalog", t => t.HasCheckConstraint("CK_PlanPrices_Amount", "[Amount] >= 0"));
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.HasIndex(x => new { x.PlanId, x.BillingPeriod, x.Currency })
            .IsUnique().HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");
    }
}

public sealed class FeatureConfiguration : FullAuditEntityConfiguration<Feature>
{
    public override void Configure(EntityTypeBuilder<Feature> b)
    {
        base.Configure(b);
        b.ToTable("Features", "Catalog");
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Code).HasMaxLength(64).IsRequired();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Unit).HasMaxLength(40);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasMany(x => x.Plans).WithOne(x => x.Feature).HasForeignKey(x => x.FeatureId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PlanFeatureConfiguration : FullAuditEntityConfiguration<PlanFeature>
{
    public override void Configure(EntityTypeBuilder<PlanFeature> b)
    {
        base.Configure(b);
        b.ToTable("PlanFeatures", "Catalog");
        b.Ignore(x => x.DomainEvents);
        b.HasIndex(x => new { x.PlanId, x.FeatureId }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
