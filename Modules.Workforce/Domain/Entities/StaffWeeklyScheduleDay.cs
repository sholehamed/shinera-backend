namespace Modules.System.Workforce.Domain.Entities;

public sealed class StaffWeeklyScheduleDay : AuditableEntity, IMustHaveTenant
{
    public StaffWeeklyScheduleDay() : base(31)
    {
    }

    public StaffWeeklyScheduleDay(
        Guid tenantId,
        Guid staffId,
        DayOfWeek dayOfWeek,
        bool isDayOff,
        TimeOnly? startTime,
        TimeOnly? endTime) : base(31)
    {
        TenantId = tenantId;
        StaffId = staffId;
        DayOfWeek = dayOfWeek;
        IsDayOff = isDayOff;
        StartTime = isDayOff ? null : startTime;
        EndTime = isDayOff ? null : endTime;
    }

    public Guid TenantId { get; set; }
    public Guid StaffId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsDayOff { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public Staff Staff { get; set; } = default!;
    public ICollection<StaffScheduleBreak> Breaks { get; set; } = [];

    public void Configure(
        bool isDayOff,
        TimeOnly? startTime,
        TimeOnly? endTime)
    {
        IsDayOff = isDayOff;
        StartTime = isDayOff ? null : startTime;
        EndTime = isDayOff ? null : endTime;
    }
}
