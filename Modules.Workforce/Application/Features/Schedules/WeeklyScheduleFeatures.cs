using Modules.System.Identity.Application.Authorization;
using Modules.System.Workforce.Application.Abstractions;
using Modules.System.Workforce.Application.Authorization;
using Modules.System.Workforce.Domain.Entities;

namespace Modules.System.Workforce.Application.Features.Schedules;

public sealed record StaffScheduleBreakDto(
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record StaffScheduleDayDto(
    DayOfWeek DayOfWeek,
    bool IsDayOff,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    IReadOnlyList<StaffScheduleBreakDto> Breaks);

public sealed record StaffWeeklyScheduleDto(
    Guid StaffId,
    IReadOnlyList<StaffScheduleDayDto> Days);

public sealed record StaffScheduleBreakInput(
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record StaffScheduleDayInput(
    DayOfWeek DayOfWeek,
    bool IsDayOff,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    IReadOnlyCollection<StaffScheduleBreakInput>? Breaks);

public sealed record StaffWeeklyScheduleQuery(Guid StaffId)
    : IQuery<StaffWeeklyScheduleDto>;

public sealed class StaffWeeklyScheduleQueryHandler(
    IWorkforceDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : IQueryHandler<
        StaffWeeklyScheduleQuery,
        StaffWeeklyScheduleDto>
{
    public async Task<StaffWeeklyScheduleDto> Handle(
        StaffWeeklyScheduleQuery query,
        CancellationToken cancellationToken)
    {
        await WorkforcePermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Staff.ViewSchedule,
            cancellationToken);

        var staffExists = await db.Staff
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == query.StaffId,
                cancellationToken);

        if (!staffExists)
        {
            throw new NotFoundException(
                nameof(Staff),
                query.StaffId.ToString());
        }

        var days = await db.StaffWeeklyScheduleDays
            .AsNoTracking()
            .Where(x => x.StaffId == query.StaffId)
            .Select(x => new StaffScheduleDayDto(
                x.DayOfWeek,
                x.IsDayOff,
                x.StartTime,
                x.EndTime,
                x.Breaks
                    .OrderBy(scheduleBreak =>
                        scheduleBreak.StartTime)
                    .Select(scheduleBreak =>
                        new StaffScheduleBreakDto(
                            scheduleBreak.StartTime,
                            scheduleBreak.EndTime))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new StaffWeeklyScheduleDto(
            query.StaffId,
            days
                .OrderBy(DayDisplayOrder)
                .ToArray());
    }

    private static int DayDisplayOrder(
        StaffScheduleDayDto day) =>
        day.DayOfWeek switch
        {
            DayOfWeek.Saturday => 0,
            DayOfWeek.Sunday => 1,
            DayOfWeek.Monday => 2,
            DayOfWeek.Tuesday => 3,
            DayOfWeek.Wednesday => 4,
            DayOfWeek.Thursday => 5,
            DayOfWeek.Friday => 6,
            _ => 99
        };
    }
}

public sealed record ReplaceStaffWeeklyScheduleCommand(
    Guid StaffId,
    IReadOnlyCollection<StaffScheduleDayInput>? Days)
    : ICommand;

public sealed class ReplaceStaffWeeklyScheduleCommandValidator
    : AbstractValidator<ReplaceStaffWeeklyScheduleCommand>
{
    public ReplaceStaffWeeklyScheduleCommandValidator()
    {
        RuleFor(x => x.StaffId)
            .NotEmpty();

        RuleFor(x => x.Days)
            .NotNull()
            .Must(days => days is not null && days.Count == 7)
            .WithMessage(
                "A complete weekly schedule must contain exactly seven days.")
            .Must(HaveEveryDayExactlyOnce)
            .WithMessage(
                "Each day of the week must appear exactly once.");

        RuleFor(x => x.Days)
            .Custom(ValidateDays);
    }

    private static bool HaveEveryDayExactlyOnce(
        IReadOnlyCollection<StaffScheduleDayInput>? days)
    {
        if (days is null || days.Count != 7)
            return false;

        return days.All(x => Enum.IsDefined(x.DayOfWeek))
            && days.Select(x => x.DayOfWeek).Distinct().Count() == 7;
    }

    private static void ValidateDays(
        IReadOnlyCollection<StaffScheduleDayInput>? days,
        ValidationContext<ReplaceStaffWeeklyScheduleCommand> context)
    {
        if (days is null)
            return;

        var materialized = days.ToArray();

        for (var index = 0; index < materialized.Length; index++)
        {
            var day = materialized[index];
            var property = $"Days[{index}]";

            if (!Enum.IsDefined(day.DayOfWeek))
            {
                context.AddFailure(
                    $"{property}.DayOfWeek",
                    "DayOfWeek is invalid.");
                continue;
            }

            var breaks = day.Breaks?.ToArray() ?? [];

            if (day.IsDayOff)
            {
                if (day.StartTime.HasValue ||
                    day.EndTime.HasValue ||
                    breaks.Length > 0)
                {
                    context.AddFailure(
                        property,
                        "A day off cannot contain working hours or breaks.");
                }

                continue;
            }

            if (!day.StartTime.HasValue ||
                !day.EndTime.HasValue)
            {
                context.AddFailure(
                    property,
                    "A working day requires both StartTime and EndTime.");
                continue;
            }

            if (day.StartTime.Value >= day.EndTime.Value)
            {
                context.AddFailure(
                    property,
                    "StartTime must be before EndTime.");
                continue;
            }

            for (var breakIndex = 0;
                 breakIndex < breaks.Length;
                 breakIndex++)
            {
                var scheduleBreak = breaks[breakIndex];
                var breakProperty =
                    $"{property}.Breaks[{breakIndex}]";

                if (scheduleBreak.StartTime >=
                    scheduleBreak.EndTime)
                {
                    context.AddFailure(
                        breakProperty,
                        "Break StartTime must be before EndTime.");
                    continue;
                }

                if (scheduleBreak.StartTime <
                        day.StartTime.Value ||
                    scheduleBreak.EndTime >
                        day.EndTime.Value)
                {
                    context.AddFailure(
                        breakProperty,
                        "Breaks must be fully inside working hours.");
                }
            }

            var orderedBreaks = breaks
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.EndTime)
                .ToArray();

            for (var breakIndex = 1;
                 breakIndex < orderedBreaks.Length;
                 breakIndex++)
            {
                if (orderedBreaks[breakIndex].StartTime <
                    orderedBreaks[breakIndex - 1].EndTime)
                {
                    context.AddFailure(
                        $"{property}.Breaks",
                        "Schedule breaks cannot overlap.");
                    break;
                }
            }
        }
    }
}

public sealed class ReplaceStaffWeeklyScheduleCommandHandler(
    IWorkforceDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : ICommandHandler<ReplaceStaffWeeklyScheduleCommand>
{
    public async Task Handle(
        ReplaceStaffWeeklyScheduleCommand command,
        CancellationToken cancellationToken)
    {
        await WorkforcePermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Staff.UpdateSchedule,
            cancellationToken);

        var tenantId = currentTenant.TenantId
            ?? throw new TenantAccessException(
                "tenant.context_missing",
                "A tenant context is required for workforce operations.");

        var staffExists = await db.Staff
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == command.StaffId,
                cancellationToken);

        if (!staffExists)
        {
            throw new NotFoundException(
                nameof(Staff),
                command.StaffId.ToString());
        }

        var requestedDays =
            (command.Days ?? [])
                .ToDictionary(x => x.DayOfWeek);

        var existingDays = await db.StaffWeeklyScheduleDays
            .Include(x => x.Breaks)
            .Where(x => x.StaffId == command.StaffId)
            .ToListAsync(cancellationToken);

        foreach (var existingDay in existingDays)
        {
            if (!requestedDays.TryGetValue(
                    existingDay.DayOfWeek,
                    out var requestedDay))
            {
                db.StaffWeeklyScheduleDays.Remove(
                    existingDay);
                continue;
            }

            existingDay.Configure(
                requestedDay.IsDayOff,
                requestedDay.StartTime,
                requestedDay.EndTime);

            SynchronizeBreaks(
                db,
                tenantId,
                existingDay,
                requestedDay.Breaks ?? []);
        }

        var existingDaySet = existingDays
            .Select(x => x.DayOfWeek)
            .ToHashSet();

        foreach (var requestedDay in requestedDays.Values)
        {
            if (existingDaySet.Contains(
                    requestedDay.DayOfWeek))
            {
                continue;
            }

            var scheduleDay =
                new StaffWeeklyScheduleDay(
                    tenantId,
                    command.StaffId,
                    requestedDay.DayOfWeek,
                    requestedDay.IsDayOff,
                    requestedDay.StartTime,
                    requestedDay.EndTime);

            foreach (var scheduleBreak in
                     requestedDay.Breaks ?? [])
            {
                scheduleDay.Breaks.Add(
                    new StaffScheduleBreak(
                        tenantId,
                        scheduleDay.Id,
                        scheduleBreak.StartTime,
                        scheduleBreak.EndTime));
            }

            db.StaffWeeklyScheduleDays.Add(
                scheduleDay);
        }

        await db.SaveChangesAsync(
            cancellationToken);
    }

    private static void SynchronizeBreaks(
        IWorkforceDbContext db,
        Guid tenantId,
        StaffWeeklyScheduleDay existingDay,
        IReadOnlyCollection<StaffScheduleBreakInput> requestedBreaks)
    {
        var requestedSet = requestedBreaks
            .Select(x => (
                x.StartTime,
                x.EndTime))
            .ToHashSet();

        var existingSet = existingDay.Breaks
            .Select(x => (
                x.StartTime,
                x.EndTime))
            .ToHashSet();

        db.StaffScheduleBreaks.RemoveRange(
            existingDay.Breaks.Where(x =>
                !requestedSet.Contains((
                    x.StartTime,
                    x.EndTime))));

        foreach (var scheduleBreak in requestedBreaks)
        {
            if (existingSet.Contains((
                    scheduleBreak.StartTime,
                    scheduleBreak.EndTime)))
            {
                continue;
            }

            existingDay.Breaks.Add(
                new StaffScheduleBreak(
                    tenantId,
                    existingDay.Id,
                    scheduleBreak.StartTime,
                    scheduleBreak.EndTime));
        }
    }
}
