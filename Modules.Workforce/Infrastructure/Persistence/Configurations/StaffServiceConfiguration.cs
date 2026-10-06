using Modules.System.Workforce.Domain.Entities;

namespace Modules.System.Workforce.Infrastructure.Persistence.Configurations;

internal sealed class StaffServiceConfiguration
    : AuditEntityConfiguration<StaffService>
{
    public override void Configure(
        EntityTypeBuilder<StaffService> builder)
    {
        builder.ToTable("StaffServices");

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.StaffId,
            x.ServiceId
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.ServiceId
        });

        builder.HasOne(x => x.Staff)
            .WithMany(x => x.Services)
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
