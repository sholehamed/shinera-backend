namespace Modules.System.Appointments.Domain;

using Modules.System.Appointments.Domain.Entities;

public static class AppointmentBookingRules
{
    public static readonly AppointmentStatus[] BlockingStatuses =
    [
        AppointmentStatus.Pending,
        AppointmentStatus.Confirmed,
        AppointmentStatus.Upcoming,
        AppointmentStatus.InProgress
    ];
}
