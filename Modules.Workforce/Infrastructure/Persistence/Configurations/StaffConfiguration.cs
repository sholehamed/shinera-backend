using Modules.System.Workforce.Domain.Entities;

namespace Modules.System.Workforce.Infrastructure.Persistence.Configurations;

internal sealed class StaffConfiguration
    : AuditEntityConfiguration<Staff>
{
    public override void Configure(
        EntityTypeBuilder<Staff> builder)
    {
        builder.ToTable("Staff");

        builder.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasAlternateKey(x => new
        {
            x.Id,
            x.TenantId
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.IsActive
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.UserId
        })
        .IsUnique()
        .HasFilter("[UserId] IS NOT NULL")
        .HasDatabaseName("UX_Staff_TenantId_UserId");

        base.Configure(builder);
    }
}
