using Modules.System.Workforce.Domain.Entities;

namespace Modules.System.Workforce.Infrastructure.Persistence.Configurations;

internal sealed class StaffWeeklyScheduleDayConfiguration
    : AuditEntityConfiguration<StaffWeeklyScheduleDay>
{
    public override void Configure(
        EntityTypeBuilder<StaffWeeklyScheduleDay> builder)
    {
        builder.ToTable(
            "StaffWeeklyScheduleDays",
            table => table.HasCheckConstraint(
                "CK_StaffWeeklyScheduleDays_WorkingHours",
                "([IsDayOff] = 1 AND [StartTime] IS NULL AND [EndTime] IS NULL) OR " +
                "([IsDayOff] = 0 AND [StartTime] IS NOT NULL AND [EndTime] IS NOT NULL AND [StartTime] < [EndTime])"));

        builder.Property(x => x.DayOfWeek)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IsDayOff)
            .IsRequired();

        builder.Property(x => x.StartTime)
            .HasColumnType("time");

        builder.Property(x => x.EndTime)
            .HasColumnType("time");

        builder.HasAlternateKey(x => new
        {
            x.Id,
            x.TenantId
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.StaffId,
            x.DayOfWeek
        })
        .IsUnique();

        builder.HasOne(x => x.Staff)
            .WithMany(x => x.WeeklySchedule)
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
