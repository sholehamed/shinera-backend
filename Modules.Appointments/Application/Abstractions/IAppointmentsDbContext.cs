using Microsoft.EntityFrameworkCore.Infrastructure;
using Modules.System.Appointments.Domain.Entities;

namespace Modules.System.Appointments.Application.Abstractions;

public interface IAppointmentsDbContext : IBaseDbContext
{
    DatabaseFacade Database { get; }
    DbSet<Appointment> Appointments { get; }
}
