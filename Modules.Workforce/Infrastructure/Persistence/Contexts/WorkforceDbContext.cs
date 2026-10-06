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
    }
}
