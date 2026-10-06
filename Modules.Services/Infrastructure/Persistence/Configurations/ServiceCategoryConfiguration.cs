using Modules.System.Services.Domain.Entities;

namespace Modules.System.Services.Infrastructure.Persistence.Configurations;

internal sealed class ServiceCategoryConfiguration
    : AuditEntityConfiguration<ServiceCategory>
{
    public override void Configure(
        EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ToTable("ServiceCategories");

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => new
            {
                x.TenantId,
                x.IsActive,
                x.SortOrder
            });

        builder.HasAlternateKey(x => new
            {
                x.Id,
                x.TenantId
            });

        base.Configure(builder);
    }
}
