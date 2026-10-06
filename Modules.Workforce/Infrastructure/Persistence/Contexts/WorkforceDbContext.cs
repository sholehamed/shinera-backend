using Modules.System.Workforce.Application.Abstractions;
using Modules.System.Workforce.Domain.Entities;
using System.Reflection;

namespace Modules.System.Workforce.Infrastructure.Persistence.Contexts;

public class WorkforceDbContext(
    DbContextOptions<WorkforceDbContext> options,
    ICurrentTenant currentTenant)
    : BaseDbContext(options), IWorkforceDbContext
{
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<StaffBranch> StaffBranches => Set<StaffBranch>();
    public DbSet<StaffService> StaffServices => Set<StaffService>();
    public DbSet<StaffWeeklyScheduleDay> StaffWeeklyScheduleDays =>
        Set<StaffWeeklyScheduleDay>();
    public DbSet<StaffScheduleBreak> StaffScheduleBreaks =>
        Set<StaffScheduleBreak>();

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

        builder.Entity<Staff>()
            .HasQueryFilter(
                "tenant",
                staff =>
                    FilterDisabled ||
                    staff.TenantId == CurrentTenantId);

        builder.Entity<StaffBranch>()
            .HasQueryFilter(
                "tenant",
                link =>
                    FilterDisabled ||
                    link.TenantId == CurrentTenantId);

        builder.Entity<StaffService>()
            .HasQueryFilter(
                "tenant",
                link =>
                    FilterDisabled ||
                    link.TenantId == CurrentTenantId);

        builder.Entity<StaffWeeklyScheduleDay>()
            .HasQueryFilter(
                "tenant",
                day =>
                    FilterDisabled ||
                    day.TenantId == CurrentTenantId);

        builder.Entity<StaffScheduleBreak>()
            .HasQueryFilter(
                "tenant",
                scheduleBreak =>
                    FilterDisabled ||
                    scheduleBreak.TenantId == CurrentTenantId);
    }
}
