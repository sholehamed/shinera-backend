using Modules.System.Crm.Application.Abstractions;
using Modules.System.Crm.Domain.Entities;
using System.Reflection;

namespace Modules.System.Crm.Infrastructure.Persistence.Contexts;

public class CrmDbContext(
    DbContextOptions<CrmDbContext> options,
    ICurrentTenant currentTenant)
    : BaseDbContext(options), ICrmDbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerNote> CustomerNotes => Set<CustomerNote>();

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

        builder.Entity<Customer>()
            .HasQueryFilter(
                "tenant",
                customer =>
                    FilterDisabled ||
                    customer.TenantId == CurrentTenantId);

        builder.Entity<CustomerNote>()
            .HasQueryFilter(
                "tenant",
                note =>
                    FilterDisabled ||
                    note.TenantId == CurrentTenantId);
    }
}
