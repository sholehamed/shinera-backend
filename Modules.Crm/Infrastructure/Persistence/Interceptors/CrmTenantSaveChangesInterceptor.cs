using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Modules.System.Crm.Infrastructure.Persistence.Interceptors;

public sealed class CrmTenantSaveChangesInterceptor(
    ICurrentTenant currentTenant)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ValidateAndStamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ValidateAndStamp(eventData.Context);

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }

    private void ValidateAndStamp(DbContext? context)
    {
        if (context is null ||
            currentTenant.IsFilterDisabled)
        {
            return;
        }

        var entries = context.ChangeTracker
            .Entries<IMustHaveTenant>()
            .Where(IsChanged)
            .ToArray();

        if (entries.Length == 0)
            return;

        var tenantId = currentTenant.TenantId
            ?? throw new TenantAccessException(
                "tenant.context_missing",
                "A tenant context is required for CRM operations.");

        if (!currentTenant.WritableTenantIds.Contains(tenantId))
        {
            throw new TenantAccessException(
                "tenant.write_denied",
                "The current user cannot write to the active tenant.");
        }

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.TenantId == Guid.Empty)
                {
                    entry.Entity.TenantId = tenantId;
                }
                else if (entry.Entity.TenantId != tenantId)
                {
                    throw new TenantAccessException(
                        "tenant.cross_tenant_write",
                        "CRM data cannot be created for another tenant.");
                }

                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                var tenantProperty =
                    entry.Property(
                        nameof(IMustHaveTenant.TenantId));

                var originalTenantId =
                    (Guid)tenantProperty.OriginalValue!;

                if (originalTenantId !=
                    entry.Entity.TenantId)
                {
                    throw new TenantAccessException(
                        "tenant.reassignment_forbidden",
                        "CRM tenant ownership cannot be reassigned.");
                }
            }

            if (entry.Entity.TenantId != tenantId)
            {
                throw new TenantAccessException(
                    "tenant.cross_tenant_write",
                    "CRM data cannot be modified or deleted from another tenant.");
            }
        }
    }

    private static bool IsChanged(EntityEntry entry) =>
        entry.State is not EntityState.Detached
            and not EntityState.Unchanged;
}
