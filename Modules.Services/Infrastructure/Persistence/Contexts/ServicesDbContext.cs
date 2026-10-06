using Modules.System.Services.Application.Abstractions;
using Modules.System.Services.Domain.Entities;
using System.Reflection;
using ServiceEntity = Modules.System.Services.Domain.Entities.Service;

namespace Modules.System.Services.Infrastructure.Persistence.Contexts;

public class ServicesDbContext(
    DbContextOptions<ServicesDbContext> options,
    ICurrentTenant currentTenant)
    : BaseDbContext(options), IServiceCatalogDbContext
{
    public DbSet<ServiceCategory> ServiceCategories =>
        Set<ServiceCategory>();

    public DbSet<ServiceEntity> Services =>
        Set<ServiceEntity>();

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

        builder.Entity<ServiceCategory>()
            .HasQueryFilter(
                "tenant",
                category =>
                    FilterDisabled ||
                    category.TenantId == CurrentTenantId);

        builder.Entity<ServiceEntity>()
            .HasQueryFilter(
                "tenant",
                service =>
                    FilterDisabled ||
                    service.TenantId == CurrentTenantId);
    }
}
