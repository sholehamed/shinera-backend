using Infrastructure.SharedKernel.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Infrastructure.Persistence.Configurations;

internal sealed class BusinessProfileConfiguration
    : AuditEntityConfiguration<BusinessProfile>
{
    public override void Configure(EntityTypeBuilder<BusinessProfile> builder)
    {
        builder.ToTable("BusinessProfiles");

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.BusinessType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Mode)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasMaxLength(32);

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.City)
            .HasMaxLength(150);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.TenantId)
            .IsUnique();

        builder.HasOne(x => x.Tenant)
            .WithOne(x => x.BusinessProfile)
            .HasForeignKey<BusinessProfile>(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        base.Configure(builder);
    }
}
