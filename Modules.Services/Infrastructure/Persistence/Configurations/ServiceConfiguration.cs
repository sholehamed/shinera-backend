using ServiceEntity = Modules.System.Services.Domain.Entities.Service;

namespace Modules.System.Services.Infrastructure.Persistence.Configurations;

internal sealed class ServiceConfiguration
    : AuditEntityConfiguration<ServiceEntity>
{
    public override void Configure(
        EntityTypeBuilder<ServiceEntity> builder)
    {
        builder.ToTable("Services");

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.DurationMinutes)
            .IsRequired();

        builder.Property(x => x.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => new
            {
                x.TenantId,
                x.CategoryId,
                x.IsActive
            });

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Services)
            .HasForeignKey(x => new
            {
                x.CategoryId,
                x.TenantId
            })
            .HasPrincipalKey(x => new
            {
                x.Id,
                x.TenantId
            })
            .OnDelete(DeleteBehavior.Restrict);

        base.Configure(builder);
    }
}
