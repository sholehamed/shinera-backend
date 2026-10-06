using Modules.System.Appointments.Application.Abstractions;
using Modules.System.Appointments.Application.Authorization;
using Modules.System.Appointments.Application.Availability;
using Modules.System.Appointments.Domain;
using Modules.System.Appointments.Domain.Entities;
using Modules.System.Crm.Application.Abstractions;
using Modules.System.Identity.Application.Authorization;

namespace Modules.System.Appointments.Application.Features.Appointments;

public sealed record AvailableSlotsQuery(
    Guid BranchId,
    Guid ServiceId,
    Guid? StaffId,
    DateOnly Date)
    : IQuery<Result<IReadOnlyList<TimeOnly>>>;

public sealed class AvailableSlotsQueryValidator
    : AbstractValidator<AvailableSlotsQuery>
{
    public AvailableSlotsQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.StaffId)
            .Must(x => !x.HasValue || x.Value != Guid.Empty);
        RuleFor(x => x.Date)
            .NotEqual(default(DateOnly));
    }
}

public sealed class AvailableSlotsQueryHandler(
    IAppointmentAvailabilityService availabilityService,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : IQueryHandler<
        AvailableSlotsQuery,
        Result<IReadOnlyList<TimeOnly>>>
{
    public async Task<Result<IReadOnlyList<TimeOnly>>> Handle(
        AvailableSlotsQuery query,
        CancellationToken cancellationToken)
    {
        await AppointmentPermissionGuard.RequireBranchAccessAsync(
            authorizationService,
            currentTenant,
            query.BranchId,
            SystemPermissionCatalog.Appointments.View,
            cancellationToken);

        return await availabilityService.GetAvailableSlotsAsync(
            query.BranchId,
            query.ServiceId,
            query.StaffId,
            query.Date,
            cancellationToken);
    }
}

public sealed record CreateAppointmentCommand(
    Guid BranchId,
    Guid CustomerId,
    Guid StaffId,
    Guid ServiceId,
    DateOnly Date,
    TimeOnly StartTime,
    string? Notes)
    : ICommand<Result<Guid>>;

public sealed class CreateAppointmentCommandValidator
    : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.StaffId).NotEmpty();
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.Date)
            .NotEqual(default(DateOnly));
        RuleFor(x => x.Notes)
            .MaximumLength(2000);
    }
}

public sealed class CreateAppointmentCommandHandler(
    IAppointmentsDbContext db,
    ICrmDbContext crmDb,
    IAppointmentAvailabilityService availabilityService,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService,
    IStaffBookingConcurrencyGuard concurrencyGuard)
    : ICommandHandler<CreateAppointmentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        await AppointmentPermissionGuard.RequireBranchAccessAsync(
            authorizationService,
            currentTenant,
            command.BranchId,
            SystemPermissionCatalog.Appointments.Create,
            cancellationToken);

        var tenantId = currentTenant.TenantId
            ?? throw new TenantAccessException(
                "tenant.context_missing",
                "A tenant context is required for appointment operations.");

        await using var transaction =
            await db.Database.BeginTransactionAsync(
                cancellationToken);

        bool staffLocked;

        try
        {
            staffLocked =
                await concurrencyGuard.TryAcquireAsync(
                    db.Database,
                    tenantId,
                    command.StaffId,
                    cancellationToken);
        }
        catch (StaffBookingConcurrencyException ex)
        {
            throw new StaffBookingConcurrencyException(
                "appointment.conflict",
                "The selected time is no longer available. Please choose another time.",
                ex);
        }

        if (!staffLocked)
        {
            return Result<Guid>.Failure(
                Error.Validation(
                    "appointment.staff_unavailable",
                    "The selected staff member is not available for the current tenant."));
        }

        var customerIsActive = await crmDb.Customers
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.Id == command.CustomerId &&
                    x.IsActive,
                cancellationToken);

        if (!customerIsActive)
        {
            return Result<Guid>.Failure(
                Error.Validation(
                    "appointment.customer_invalid",
                    "The selected customer is not active or is not available for the current tenant."));
        }

        var slot = await availabilityService.ValidateSlotAsync(
            command.BranchId,
            command.ServiceId,
            command.StaffId,
            command.Date,
            command.StartTime,
            cancellationToken);

        if (slot.IsFailure)
        {
            return Result<Guid>.Failure(slot.Error);
        }

        var hasConflict = await db.Appointments
            .AnyAsync(
                x =>
                    x.StaffId == command.StaffId &&
                    x.Date == command.Date &&
                    AppointmentBookingRules.BlockingStatuses.Contains(x.Status) &&
                    x.StartUtc < slot.Value.EndUtc &&
                    x.EndUtc > slot.Value.StartUtc,
                cancellationToken);

        if (hasConflict)
        {
            return Result<Guid>.Failure(
                Error.Conflict(
                    "appointment.conflict",
                    "The selected time overlaps another appointment for this staff member."));
        }

        var appointment = new Appointment(
            tenantId,
            command.BranchId,
            command.CustomerId,
            command.StaffId,
            command.ServiceId,
            command.Date,
            command.StartTime,
            slot.Value.EndTime,
            slot.Value.StartUtc,
            slot.Value.EndUtc,
            slot.Value.TimeZoneId,
            slot.Value.Price,
            command.Notes,
            AppointmentStatus.Confirmed);

        db.Appointments.Add(appointment);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<Guid>.Success(appointment.Id);
    }
}
