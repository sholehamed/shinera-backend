using Modules.System.Crm.Domain.Entities;

namespace Modules.System.Crm.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration
    : AuditEntityConfiguration<Customer>
{
    public override void Configure(
        EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Mobile)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.NormalizedMobile)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.Birthday)
            .HasColumnType("date");

        builder.Property(x => x.Gender)
            .HasMaxLength(32);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.IsVip)
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
            x.NormalizedMobile
        })
        .IsUnique()
        .HasDatabaseName(
            "UX_Customers_TenantId_NormalizedMobile");

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.UserId
        })
        .IsUnique()
        .HasFilter("[UserId] IS NOT NULL")
        .HasDatabaseName(
            "UX_Customers_TenantId_UserId");

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.IsActive
        });

        base.Configure(builder);
    }
}
