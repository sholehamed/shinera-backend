using Infrastructure.SharedKernel.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Infrastructure.Persistence.Configurations;

internal sealed class TenantMembershipConfiguration
    : AuditEntityConfiguration<TenantMembership>
{
    public override void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("TenantMemberships");

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => new { x.TenantId, x.UserId })
            .IsUnique();

        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithMany(x => x.TenantMemberships)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.Memberships)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        base.Configure(builder);
    }
}
