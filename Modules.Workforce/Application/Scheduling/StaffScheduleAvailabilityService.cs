using Modules.System.Workforce.Application.Abstractions;

namespace Modules.System.Workforce.Application.Scheduling;

public sealed record ScheduleTimeSegment(
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record StaffDailyAvailability(
    Guid StaffId,
    DateOnly Date,
    bool IsConfigured,
    bool IsStaffActive,
    bool IsDayOff,
    IReadOnlyList<ScheduleTimeSegment> AvailableSegments);

public interface IStaffScheduleAvailabilityService
{
    Task<StaffDailyAvailability> ResolveAsync(
        Guid staffId,
        DateOnly date,
        CancellationToken cancellationToken = default);
}

public sealed class StaffScheduleAvailabilityService(
    IWorkforceDbContext db)
    : IStaffScheduleAvailabilityService
{
    public async Task<StaffDailyAvailability> ResolveAsync(
        Guid staffId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var staff = await db.Staff
            .AsNoTracking()
            .Where(x => x.Id == staffId)
            .Select(x => new
            {
                x.Id,
                x.IsActive
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (staff is null)
        {
            return new StaffDailyAvailability(
                staffId,
                date,
                false,
                false,
                false,
                []);
        }

        var schedule = await db.StaffWeeklyScheduleDays
            .AsNoTracking()
            .Where(x =>
                x.StaffId == staffId &&
                x.DayOfWeek == date.DayOfWeek)
            .Select(x => new
            {
                x.IsDayOff,
                x.StartTime,
                x.EndTime,
                Breaks = x.Breaks
                    .OrderBy(b => b.StartTime)
                    .Select(b => new
                    {
                        b.StartTime,
                        b.EndTime
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (schedule is null)
        {
            return new StaffDailyAvailability(
                staff.Id,
                date,
                false,
                staff.IsActive,
                false,
                []);
        }

        if (!staff.IsActive ||
            schedule.IsDayOff ||
            !schedule.StartTime.HasValue ||
            !schedule.EndTime.HasValue)
        {
            return new StaffDailyAvailability(
                staff.Id,
                date,
                true,
                staff.IsActive,
                schedule.IsDayOff,
                []);
        }

        var segments = new List<ScheduleTimeSegment>();
        var cursor = schedule.StartTime.Value;

        foreach (var scheduleBreak in schedule.Breaks)
        {
            if (cursor < scheduleBreak.StartTime)
            {
                segments.Add(
                    new ScheduleTimeSegment(
                        cursor,
                        scheduleBreak.StartTime));
            }

            cursor = scheduleBreak.EndTime;
        }

        if (cursor < schedule.EndTime.Value)
        {
            segments.Add(
                new ScheduleTimeSegment(
                    cursor,
                    schedule.EndTime.Value));
        }

        return new StaffDailyAvailability(
            staff.Id,
            date,
            true,
            staff.IsActive,
            false,
            segments);
    }
}
