using Modules.System.Appointments.Domain.Entities;

namespace Modules.System.Appointments.Infrastructure.Persistence.Configurations;

internal sealed class AppointmentConfiguration
    : AuditEntityConfiguration<Appointment>
{
    public override void Configure(
        EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable(
            "Appointments",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Appointments_TimeRange",
                    "[StartTime] < [EndTime]");

                table.HasCheckConstraint(
                    "CK_Appointments_Price",
                    "[Price] >= 0");

                table.HasCheckConstraint(
                    "CK_Appointments_Status",
                    "[Status] >= 1 AND [Status] <= 7");
            });

        builder.Property(x => x.Date)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.StartTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(x => x.EndTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(x => x.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.HasAlternateKey(x => new
        {
            x.Id,
            x.TenantId
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.BranchId,
            x.Date
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.CustomerId,
            x.Date
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.StaffId,
            x.Date,
            x.StartTime,
            x.EndTime
        })
        .HasDatabaseName(
            "IX_Appointments_Tenant_Staff_Date_Time");

        base.Configure(builder);
    }
}
