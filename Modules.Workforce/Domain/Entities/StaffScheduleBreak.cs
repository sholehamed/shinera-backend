namespace Modules.System.Workforce.Domain.Entities;

public sealed class StaffScheduleBreak : AuditableEntity, IMustHaveTenant
{
    public StaffScheduleBreak() : base(32)
    {
    }

    public StaffScheduleBreak(
        Guid tenantId,
        Guid scheduleDayId,
        TimeOnly startTime,
        TimeOnly endTime) : base(32)
    {
        TenantId = tenantId;
        ScheduleDayId = scheduleDayId;
        StartTime = startTime;
        EndTime = endTime;
    }

    public Guid TenantId { get; set; }
    public Guid ScheduleDayId { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public StaffWeeklyScheduleDay ScheduleDay { get; set; } = default!;
}
