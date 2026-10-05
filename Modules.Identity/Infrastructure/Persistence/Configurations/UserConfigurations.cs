using Infrastructure.SharedKernel.Persistence.Configurations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfigurations : AuditEntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(x => x.NormalizedUserName)
            .IsUnique();

        builder.HasIndex(x => x.NormalizedEmail)
            .IsUnique();

        builder.Property(x => x.UserName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.NormalizedUserName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.NormalizedEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .IsRequired();

        base.Configure(builder);
    }
}
