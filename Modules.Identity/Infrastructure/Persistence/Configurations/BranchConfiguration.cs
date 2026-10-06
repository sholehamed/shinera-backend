using Infrastructure.SharedKernel.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Infrastructure.Persistence.Configurations;

internal sealed class BranchConfiguration
    : AuditEntityConfiguration<Branch>
{
    public override void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasMaxLength(32);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.IsMain)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => new { x.TenantId, x.IsActive });

        builder.HasIndex(x => x.TenantId)
            .IsUnique()
            .HasFilter("[IsMain] = 1")
            .HasDatabaseName("UX_Branches_TenantId_Main");

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.Branches)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        base.Configure(builder);
    }
}
