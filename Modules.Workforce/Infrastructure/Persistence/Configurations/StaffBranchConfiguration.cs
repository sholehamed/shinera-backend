using Modules.System.Workforce.Domain.Entities;

namespace Modules.System.Workforce.Infrastructure.Persistence.Configurations;

internal sealed class StaffBranchConfiguration
    : AuditEntityConfiguration<StaffBranch>
{
    public override void Configure(
        EntityTypeBuilder<StaffBranch> builder)
    {
        builder.ToTable("StaffBranches");

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.StaffId,
            x.BranchId
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.BranchId
        });

        builder.HasOne(x => x.Staff)
            .WithMany(x => x.Branches)
            .HasForeignKey(x => new
            {
                x.StaffId,
                x.TenantId
            })
            .HasPrincipalKey(x => new
            {
                x.Id,
                x.TenantId
            })
            .OnDelete(DeleteBehavior.Cascade);

        base.Configure(builder);
    }
}
