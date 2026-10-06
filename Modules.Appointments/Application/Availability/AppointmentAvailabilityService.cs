using Modules.System.Appointments.Application.Abstractions;
using Modules.System.Appointments.Domain;
using Modules.System.Appointments.Domain.Entities;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Services.Application.Abstractions;
using Modules.System.Workforce.Application.Abstractions;
using Modules.System.Workforce.Application.Scheduling;

namespace Modules.System.Appointments.Application.Availability;

public interface IAppointmentAvailabilityService
{
    Task<Result<IReadOnlyList<TimeOnly>>> GetAvailableSlotsAsync(
        Guid branchId,
        Guid serviceId,
        Guid? staffId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<Result<AppointmentSlotValidation>> ValidateSlotAsync(
        Guid branchId,
        Guid serviceId,
        Guid staffId,
        DateOnly date,
        TimeOnly startTime,
        CancellationToken cancellationToken = default);
}

public sealed record AppointmentSlotValidation(
    TimeOnly EndTime,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string TimeZoneId,
    decimal Price);

public sealed class AppointmentAvailabilityService(
    IAppointmentsDbContext appointmentsDb,
    IIdentityDbContext identityDb,
    IServiceCatalogDbContext servicesDb,
    IWorkforceDbContext workforceDb,
    IStaffScheduleAvailabilityService staffScheduleAvailability,
    ITimeZoneResolver timeZoneResolver)
    : IAppointmentAvailabilityService
{

    public async Task<Result<IReadOnlyList<TimeOnly>>> GetAvailableSlotsAsync(
        Guid branchId,
        Guid serviceId,
        Guid? staffId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var branch = await identityDb.Branches
            .AsNoTracking()
            .Where(x => x.Id == branchId && x.IsActive)
            .Select(x => new
            {
                x.TimeZoneId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (branch is null)
        {
            return Result<IReadOnlyList<TimeOnly>>.Failure(
                Error.Validation(
                    "appointment.branch_invalid",
                    "The selected branch is not valid for the current tenant."));
        }

        if (!timeZoneResolver.IsValidIanaTimeZoneId(
                branch.TimeZoneId))
        {
            return Result<IReadOnlyList<TimeOnly>>.Failure(
                Error.Validation(
                    "appointment.branch_timezone_invalid",
                    "The selected branch does not have a valid IANA time zone."));
        }

        var service = await servicesDb.Services
            .AsNoTracking()
            .Where(x => x.Id == serviceId && x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.DurationMinutes
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (service is null)
        {
            return Result<IReadOnlyList<TimeOnly>>.Failure(
                Error.Validation(
                    "appointment.service_invalid",
                    "The selected service is not active or is not available for the current tenant."));
        }

        var eligibleStaffQuery = workforceDb.Staff
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.Branches.Any(link =>
                    link.BranchId == branchId) &&
                x.Services.Any(link =>
                    link.ServiceId == serviceId));

        if (staffId.HasValue)
        {
            eligibleStaffQuery = eligibleStaffQuery.Where(
                x => x.Id == staffId.Value);
        }

        var eligibleStaffIds = await eligibleStaffQuery
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);

        if (staffId.HasValue &&
            eligibleStaffIds.Length == 0)
        {
            return Result<IReadOnlyList<TimeOnly>>.Failure(
                Error.Validation(
                    "appointment.staff_unavailable",
                    "The selected staff member is not active for this branch and service."));
        }

        if (eligibleStaffIds.Length == 0)
        {
            return Result<IReadOnlyList<TimeOnly>>.Success([]);
        }

        var blockingAppointments = await appointmentsDb.Appointments
            .AsNoTracking()
            .Where(AppointmentBookingRules.BlockingPredicate)
            .Where(x =>
                eligibleStaffIds.Contains(x.StaffId) &&
                x.Date == date)
            .Select(x => new BusyRange(
                x.StaffId,
                x.StartUtc,
                x.EndUtc))
            .ToListAsync(cancellationToken);

        var slots = new HashSet<TimeOnly>();

        foreach (var eligibleStaffId in eligibleStaffIds)
        {
            var schedule = await staffScheduleAvailability.ResolveAsync(
                eligibleStaffId,
                date,
                cancellationToken);

            if (!schedule.IsConfigured ||
                !schedule.IsStaffActive ||
                schedule.IsDayOff)
            {
                continue;
            }

            var staffBusyRanges = blockingAppointments
                .Where(x => x.StaffId == eligibleStaffId)
                .ToArray();

            foreach (var segment in schedule.AvailableSegments)
            {
                var cursor = segment.StartTime;

                while (cursor < segment.EndTime)
                {
                    var candidate = ResolveInterval(
                        date,
                        cursor,
                        service.DurationMinutes,
                        segment.EndTime,
                        branch.TimeZoneId);

                    if (candidate.IsFailure)
                        break;

                    if (!staffBusyRanges.Any(
                            busy =>
                                busy.StartUtc < candidate.Value.EndUtc &&
                                busy.EndUtc > candidate.Value.StartUtc))
                    {
                        slots.Add(cursor);
                    }

                    cursor = candidate.Value.EndTime;
                }
            }
        }

        return Result<IReadOnlyList<TimeOnly>>.Success(
            slots.OrderBy(x => x).ToArray());
    }

    public async Task<Result<AppointmentSlotValidation>> ValidateSlotAsync(
        Guid branchId,
        Guid serviceId,
        Guid staffId,
        DateOnly date,
        TimeOnly startTime,
        CancellationToken cancellationToken = default)
    {
        var branch = await identityDb.Branches
            .AsNoTracking()
            .Where(x => x.Id == branchId && x.IsActive)
            .Select(x => new
            {
                x.TimeZoneId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (branch is null)
        {
            return Result<AppointmentSlotValidation>.Failure(
                Error.Validation(
                    "appointment.branch_invalid",
                    "The selected branch is not valid for the current tenant."));
        }

        if (!timeZoneResolver.IsValidIanaTimeZoneId(
                branch.TimeZoneId))
        {
            return Result<AppointmentSlotValidation>.Failure(
                Error.Validation(
                    "appointment.branch_timezone_invalid",
                    "The selected branch does not have a valid IANA time zone."));
        }

        var service = await servicesDb.Services
            .AsNoTracking()
            .Where(x => x.Id == serviceId && x.IsActive)
            .Select(x => new
            {
                x.DurationMinutes,
                x.Price
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (service is null)
        {
            return Result<AppointmentSlotValidation>.Failure(
                Error.Validation(
                    "appointment.service_invalid",
                    "The selected service is not active or is not available for the current tenant."));
        }

        var staffIsEligible = await workforceDb.Staff
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.Id == staffId &&
                    x.IsActive &&
                    x.Branches.Any(link =>
                        link.BranchId == branchId) &&
                    x.Services.Any(link =>
                        link.ServiceId == serviceId),
                cancellationToken);

        if (!staffIsEligible)
        {
            return Result<AppointmentSlotValidation>.Failure(
                Error.Validation(
                    "appointment.staff_unavailable",
                    "The selected staff member is not active for this branch and service."));
        }

        var schedule = await staffScheduleAvailability.ResolveAsync(
            staffId,
            date,
            cancellationToken);

        if (!schedule.IsConfigured ||
            !schedule.IsStaffActive ||
            schedule.IsDayOff)
        {
            return Result<AppointmentSlotValidation>.Failure(
                Error.Validation(
                    "appointment.outside_schedule",
                    "The selected time is outside the staff member's available schedule."));
        }

        foreach (var segment in schedule.AvailableSegments)
        {
            if (segment.StartTime > startTime ||
                segment.EndTime <= startTime)
            {
                continue;
            }

            var interval = ResolveInterval(
                date,
                startTime,
                service.DurationMinutes,
                segment.EndTime,
                branch.TimeZoneId);

            if (interval.IsFailure)
            {
                return Result<AppointmentSlotValidation>.Failure(
                    interval.Error);
            }

            return Result<AppointmentSlotValidation>.Success(
                new AppointmentSlotValidation(
                    interval.Value.EndTime,
                    interval.Value.StartUtc,
                    interval.Value.EndUtc,
                    branch.TimeZoneId,
                    service.Price));
        }

        return Result<AppointmentSlotValidation>.Failure(
            Error.Validation(
                "appointment.outside_schedule",
                "The selected time is outside the staff member's available schedule."));
    }

    private Result<ResolvedInterval> ResolveInterval(
        DateOnly date,
        TimeOnly startTime,
        int durationMinutes,
        TimeOnly segmentEndTime,
        string timeZoneId)
    {
        if (durationMinutes <= 0)
        {
            return Result<ResolvedInterval>.Failure(
                Error.Validation(
                    "appointment.time_invalid",
                    "Appointment duration must be greater than zero."));
        }

        var startUtc = timeZoneResolver.ResolveToUtc(
            date,
            startTime,
            timeZoneId);

        if (startUtc.IsFailure)
        {
            return Result<ResolvedInterval>.Failure(
                startUtc.Error);
        }

        var endUtc =
            startUtc.Value.AddMinutes(durationMinutes);

        var endLocal = timeZoneResolver.ResolveFromUtc(
            endUtc,
            timeZoneId);

        if (endLocal.IsFailure)
        {
            return Result<ResolvedInterval>.Failure(
                endLocal.Error);
        }

        if (endLocal.Value.Date != date)
        {
            return Result<ResolvedInterval>.Failure(
                Error.Validation(
                    "appointment.time_invalid",
                    "The service would finish outside the selected business date."));
        }

        if (endLocal.Value.Time <= startTime ||
            endLocal.Value.Time > segmentEndTime)
        {
            return Result<ResolvedInterval>.Failure(
                Error.Validation(
                    "appointment.outside_schedule",
                    "The selected time is outside the staff member's available schedule."));
        }

        return Result<ResolvedInterval>.Success(
            new ResolvedInterval(
                endLocal.Value.Time,
                startUtc.Value,
                endUtc));
    }

    private sealed record BusyRange(
        Guid StaffId,
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtc);

    private sealed record ResolvedInterval(
        TimeOnly EndTime,
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtc);
}
