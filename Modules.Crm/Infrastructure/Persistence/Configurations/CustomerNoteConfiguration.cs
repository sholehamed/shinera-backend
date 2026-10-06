using Modules.System.Crm.Domain.Entities;

namespace Modules.System.Crm.Infrastructure.Persistence.Configurations;

internal sealed class CustomerNoteConfiguration
    : AuditEntityConfiguration<CustomerNote>
{
    public override void Configure(
        EntityTypeBuilder<CustomerNote> builder)
    {
        builder.ToTable("CustomerNotes");

        builder.Property(x => x.Content)
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.CustomerId,
            x.CreatedAt
        });

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.NoteEntries)
            .HasForeignKey(x => new
            {
                x.CustomerId,
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
