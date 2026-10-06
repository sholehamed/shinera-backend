using Modules.System.Appointments.Application.Abstractions;
using Modules.System.Appointments.Domain.Entities;
using System.Reflection;

namespace Modules.System.Appointments.Infrastructure.Persistence.Contexts;

public class AppointmentsDbContext(
    DbContextOptions<AppointmentsDbContext> options,
    ICurrentTenant currentTenant)
    : BaseDbContext(options), IAppointmentsDbContext
{
    public DbSet<Appointment> Appointments =>
        Set<Appointment>();

    public new Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database =>
        base.Database;

    private bool FilterDisabled =>
        currentTenant.IsFilterDisabled;

    private Guid CurrentTenantId =>
        currentTenant.TenantId ?? Guid.Empty;

    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly());

        base.OnModelCreating(builder);

        builder.Entity<Appointment>()
            .HasQueryFilter(
                "tenant",
                appointment =>
                    FilterDisabled ||
                    appointment.TenantId == CurrentTenantId);
    }
}
