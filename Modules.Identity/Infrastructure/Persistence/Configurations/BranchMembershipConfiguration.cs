using Infrastructure.SharedKernel.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Infrastructure.Persistence.Configurations;

internal sealed class BranchMembershipConfiguration
    : AuditEntityConfiguration<BranchMembership>
{
    public override void Configure(EntityTypeBuilder<BranchMembership> builder)
    {
        builder.ToTable("BranchMemberships");

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => new
            {
                x.TenantId,
                x.BranchId,
                x.UserId
            })
            .IsUnique();

        builder.HasIndex(x => new
            {
                x.TenantId,
                x.UserId,
                x.IsActive
            });

        builder.HasIndex(x => x.BranchId);

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany(x => x.Memberships)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany(x => x.BranchMemberships)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        base.Configure(builder);
    }
}
