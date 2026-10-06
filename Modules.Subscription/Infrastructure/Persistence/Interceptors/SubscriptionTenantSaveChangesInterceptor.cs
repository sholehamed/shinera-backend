namespace Modules.System.Subscription.Infrastructure.Persistence.Interceptors;

public sealed class SubscriptionTenantSaveChangesInterceptor(
    ICurrentTenant currentTenant) : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
{
    public override Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> SavingChanges(
        Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData eventData,
        Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result)
    {
        ValidateAndStamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> SavingChangesAsync(
        Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData eventData,
        Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ValidateAndStamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ValidateAndStamp(DbContext? context)
    {
        if (context is null || currentTenant.IsFilterDisabled)
            return;

        var tenantId = currentTenant.TenantId
            ?? throw new InvalidOperationException(
                "An active tenant context is required for tenant-owned subscription data.");

        if (!currentTenant.WritableTenantIds.Contains(tenantId))
        {
            throw new InvalidOperationException(
                "The current tenant context does not allow writes.");
        }

        foreach (var entry in context.ChangeTracker.Entries<IMustHaveTenant>())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
                continue;

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.TenantId == Guid.Empty)
                    entry.Entity.TenantId = tenantId;
                else if (entry.Entity.TenantId != tenantId)
                    throw new InvalidOperationException(
                        "Cross-tenant subscription writes are not allowed.");

                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                var property = entry.Property(nameof(IMustHaveTenant.TenantId));
                var originalTenantId = (Guid)property.OriginalValue!;

                if (originalTenantId != entry.Entity.TenantId)
                    throw new InvalidOperationException(
                        "Subscription tenant ownership cannot be reassigned.");
            }

            if (entry.Entity.TenantId != tenantId)
            {
                throw new InvalidOperationException(
                    "Cross-tenant subscription writes are not allowed.");
            }
        }
    }
}
