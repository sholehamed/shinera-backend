using Modules.System.Workforce.Domain.Entities;

namespace Modules.System.Workforce.Infrastructure.Persistence.Configurations;

internal sealed class StaffScheduleBreakConfiguration
    : AuditEntityConfiguration<StaffScheduleBreak>
{
    public override void Configure(
        EntityTypeBuilder<StaffScheduleBreak> builder)
    {
        builder.ToTable(
            "StaffScheduleBreaks",
            table => table.HasCheckConstraint(
                "CK_StaffScheduleBreaks_TimeRange",
                "[StartTime] < [EndTime]"));

        builder.Property(x => x.StartTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(x => x.EndTime)
            .HasColumnType("time")
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.ScheduleDayId,
            x.StartTime,
            x.EndTime
        })
        .IsUnique();

        builder.HasOne(x => x.ScheduleDay)
            .WithMany(x => x.Breaks)
            .HasForeignKey(x => new
            {
                x.ScheduleDayId,
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
