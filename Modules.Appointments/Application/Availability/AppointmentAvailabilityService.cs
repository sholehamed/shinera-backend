using Modules.System.Appointments.Application.Abstractions;
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
    decimal Price);

public sealed class AppointmentAvailabilityService(
    IAppointmentsDbContext appointmentsDb,
    IIdentityDbContext identityDb,
    IServiceCatalogDbContext servicesDb,
    IWorkforceDbContext workforceDb,
    IStaffScheduleAvailabilityService staffScheduleAvailability)
    : IAppointmentAvailabilityService
{
    public async Task<Result<IReadOnlyList<TimeOnly>>> GetAvailableSlotsAsync(
        Guid branchId,
        Guid serviceId,
        Guid? staffId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var branchIsActive = await identityDb.Branches
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == branchId && x.IsActive,
                cancellationToken);

        if (!branchIsActive)
        {
            return Result<IReadOnlyList<TimeOnly>>.Failure(
                Error.Validation(
                    "appointment.branch_invalid",
                    "The selected branch is not valid for the current tenant."));
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
            .Where(x =>
                eligibleStaffIds.Contains(x.StaffId) &&
                x.Date == date &&
                x.Status != AppointmentStatus.Cancelled)
            .Select(x => new BusyRange(
                x.StaffId,
                x.StartTime,
                x.EndTime))
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

                while (TryAddMinutes(
                           cursor,
                           service.DurationMinutes,
                           out var endTime) &&
                       endTime <= segment.EndTime)
                {
                    if (!staffBusyRanges.Any(
                            busy =>
                                busy.StartTime < endTime &&
                                busy.EndTime > cursor))
                    {
                        slots.Add(cursor);
                    }

                    cursor = endTime;
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
        var branchIsActive = await identityDb.Branches
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == branchId && x.IsActive,
                cancellationToken);

        if (!branchIsActive)
        {
            return Result<AppointmentSlotValidation>.Failure(
                Error.Validation(
                    "appointment.branch_invalid",
                    "The selected branch is not valid for the current tenant."));
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

        if (!TryAddMinutes(
                startTime,
                service.DurationMinutes,
                out var endTime))
        {
            return Result<AppointmentSlotValidation>.Failure(
                Error.Validation(
                    "appointment.time_invalid",
                    "The service would finish outside the selected date."));
        }

        var schedule = await staffScheduleAvailability.ResolveAsync(
            staffId,
            date,
            cancellationToken);

        var isInsideAvailableSchedule =
            schedule.IsConfigured &&
            schedule.IsStaffActive &&
            !schedule.IsDayOff &&
            schedule.AvailableSegments.Any(
                segment =>
                    segment.StartTime <= startTime &&
                    segment.EndTime >= endTime);

        if (!isInsideAvailableSchedule)
        {
            return Result<AppointmentSlotValidation>.Failure(
                Error.Validation(
                    "appointment.outside_schedule",
                    "The selected time is outside the staff member's available schedule."));
        }

        return Result<AppointmentSlotValidation>.Success(
            new AppointmentSlotValidation(
                endTime,
                service.Price));
    }

    private static bool TryAddMinutes(
        TimeOnly startTime,
        int minutes,
        out TimeOnly endTime)
    {
        var total =
            startTime.ToTimeSpan() +
            TimeSpan.FromMinutes(minutes);

        if (minutes <= 0 ||
            total >= TimeSpan.FromDays(1))
        {
            endTime = default;
            return false;
        }

        endTime = TimeOnly.FromTimeSpan(total);
        return true;
    }

    private sealed record BusyRange(
        Guid StaffId,
        TimeOnly StartTime,
        TimeOnly EndTime);
}
