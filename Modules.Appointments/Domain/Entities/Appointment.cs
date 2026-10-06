namespace Modules.System.Appointments.Domain.Entities;

public enum AppointmentStatus
{
    Pending = 1,
    Confirmed = 2,
    Upcoming = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6,
    NoShow = 7
}

public sealed class Appointment : AuditableEntity, IMustHaveTenant
{
    public Appointment() : base(35)
    {
    }

    public Appointment(
        Guid tenantId,
        Guid branchId,
        Guid customerId,
        Guid staffId,
        Guid serviceId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string timeZoneId,
        decimal price,
        string? notes = null,
        AppointmentStatus status = AppointmentStatus.Confirmed)
        : base(35)
    {
        TenantId = tenantId;
        BranchId = branchId;
        CustomerId = customerId;
        StaffId = staffId;
        ServiceId = serviceId;
        Date = date;
        StartTime = startTime;
        EndTime = endTime;
        StartUtc = startUtc.ToUniversalTime();
        EndUtc = endUtc.ToUniversalTime();
        TimeZoneId = timeZoneId.Trim();
        Price = price;
        Notes = Normalize(notes);
        Status = status;
    }

    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid StaffId { get; set; }
    public Guid ServiceId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public string TimeZoneId { get; set; } = "Etc/UTC";
    public decimal Price { get; set; }
    public AppointmentStatus Status { get; private set; }
    public string? Notes { get; set; }

    public bool CanTransitionTo(AppointmentStatus next) =>
        Status switch
        {
            AppointmentStatus.Pending =>
                next is AppointmentStatus.Confirmed
                    or AppointmentStatus.Cancelled,

            AppointmentStatus.Confirmed =>
                next is AppointmentStatus.Upcoming
                    or AppointmentStatus.InProgress
                    or AppointmentStatus.Cancelled
                    or AppointmentStatus.NoShow,

            AppointmentStatus.Upcoming =>
                next is AppointmentStatus.InProgress
                    or AppointmentStatus.Cancelled
                    or AppointmentStatus.NoShow,

            AppointmentStatus.InProgress =>
                next is AppointmentStatus.Completed,

            _ => false
        };

    public void TransitionTo(AppointmentStatus next)
    {
        if (!CanTransitionTo(next))
        {
            throw new InvalidOperationException(
                $"Appointment cannot transition from {Status} to {next}.");
        }

        Status = next;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
