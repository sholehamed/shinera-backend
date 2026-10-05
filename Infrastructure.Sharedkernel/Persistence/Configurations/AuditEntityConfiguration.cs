using Domain.SharedKernel.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.SharedKernel.Persistence.Configurations;

public class AuditEntityConfiguration<T> : EntityBaseConfiguration<T>
    where T : class, IEntity, IAuditable
{
    public override void Configure(EntityTypeBuilder<T> builder)
    {
        base.Configure(builder);

        builder.Property(e => e.CreatedBy)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("datetimeoffset");

        builder.HasIndex(e => e.CreatedAt)
            .IsDescending();

        builder.Property(e => e.CreatedByIp)
            .HasMaxLength(45);

        builder.Property(e => e.LastModifiedBy)
            .IsRequired(false);

        builder.Property(e => e.LastModifiedAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset");

        builder.Property(e => e.LastModifiedByIp)
            .HasMaxLength(45);
    }
}
