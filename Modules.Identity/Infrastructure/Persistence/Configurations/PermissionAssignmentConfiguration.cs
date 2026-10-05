using Infrastructure.SharedKernel.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Infrastructure.Persistence.Configurations;

internal sealed class PermissionAssignmentConfiguration
    : AuditEntityConfiguration<PermissionAssignment>
{
    public override void Configure(EntityTypeBuilder<PermissionAssignment> builder)
    {
        builder.ToTable("PermissionAssignments");

        builder.Property(x => x.SubjectType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ScopeType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => new
            {
                x.TenantId,
                x.PermissionId,
                x.SubjectType,
                x.SubjectId,
                x.ScopeType,
                x.ScopeReferenceId
            })
            .IsUnique();

        builder.HasIndex(x => new
            {
                x.TenantId,
                x.SubjectType,
                x.SubjectId,
                x.IsActive
            });

        builder.HasOne(x => x.Permission)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        base.Configure(builder);
    }
}
