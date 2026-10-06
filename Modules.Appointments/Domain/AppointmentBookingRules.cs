using System.Linq.Expressions;
using Modules.System.Appointments.Domain.Entities;

namespace Modules.System.Appointments.Domain;

public static class AppointmentBookingRules
{
    public static readonly Expression<Func<Appointment, bool>>
        BlockingPredicate =
            appointment =>
                appointment.Status == AppointmentStatus.Pending ||
                appointment.Status == AppointmentStatus.Confirmed ||
                appointment.Status == AppointmentStatus.Upcoming ||
                appointment.Status == AppointmentStatus.InProgress;

    public static bool IsBlocking(
        AppointmentStatus status) =>
        status is
            AppointmentStatus.Pending or
            AppointmentStatus.Confirmed or
            AppointmentStatus.Upcoming or
            AppointmentStatus.InProgress;
}
