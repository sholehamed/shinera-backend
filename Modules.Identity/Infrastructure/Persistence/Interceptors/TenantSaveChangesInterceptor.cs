using Application.SharedKernel.Exceptions;
using Domain.SharedKernel.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Modules.System.Identity.Application.Abstractions;

namespace Modules.System.Identity.Infrastructure.Persistence.Interceptors;

public sealed class TenantSaveChangesInterceptor(
    ITenantContext tenantContext) : SaveChangesInterceptor
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
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ValidateAndStamp(DbContext? dbContext)
    {
        if (dbContext is null || tenantContext.IsFilterDisabled)
            return;

        ValidateTenantEntries(dbContext);
        ValidateBranchEntries(dbContext);
    }

    private void ValidateTenantEntries(DbContext dbContext)
    {
        var entries = dbContext.ChangeTracker
            .Entries<IMustHaveTenant>()
            .Where(IsChanged)
            .ToArray();

        if (entries.Length == 0)
            return;

        var activeTenantId = tenantContext.ActiveTenantId
            ?? throw new TenantAccessException(
                "tenant.context_missing",
                "A tenant context is required for this operation.");

        if (!tenantContext.WritableTenantIds.Contains(activeTenantId))
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
                    entry.Entity.TenantId = activeTenantId;
                }
                else if (entry.Entity.TenantId != activeTenantId)
                {
                    throw new TenantAccessException(
                        "tenant.cross_tenant_write",
                        "A tenant-owned entity cannot be created for another tenant.");
                }

                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                var tenantProperty = entry.Property(nameof(IMustHaveTenant.TenantId));
                var originalTenantId = (Guid)tenantProperty.OriginalValue!;

                if (originalTenantId != entry.Entity.TenantId)
                {
                    throw new TenantAccessException(
                        "tenant.reassignment_forbidden",
                        "Tenant ownership cannot be reassigned.");
                }
            }

            if (entry.Entity.TenantId != activeTenantId)
            {
                throw new TenantAccessException(
                    "tenant.cross_tenant_write",
                    "A tenant-owned entity cannot be modified or deleted from another tenant.");
            }
        }
    }

    private void ValidateBranchEntries(DbContext dbContext)
    {
        var entries = dbContext.ChangeTracker
            .Entries<IMustHaveBranch>()
            .Where(IsChanged)
            .ToArray();

        if (entries.Length == 0)
            return;

        var activeBranchId = tenantContext.ActiveBranchId
            ?? throw new TenantAccessException(
                "branch.context_missing",
                "A branch context is required for this operation.");

        if (!tenantContext.WritableBranchIds.Contains(activeBranchId))
        {
            throw new TenantAccessException(
                "branch.write_denied",
                "The current user cannot write to the active branch.");
        }

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.BranchId == Guid.Empty)
                {
                    entry.Entity.BranchId = activeBranchId;
                }
                else if (entry.Entity.BranchId != activeBranchId)
                {
                    throw new TenantAccessException(
                        "branch.cross_branch_write",
                        "A branch-owned entity cannot be created for another branch.");
                }

                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                var branchProperty = entry.Property(nameof(IMustHaveBranch.BranchId));
                var originalBranchId = (Guid)branchProperty.OriginalValue!;

                if (originalBranchId != entry.Entity.BranchId)
                {
                    throw new TenantAccessException(
                        "branch.reassignment_forbidden",
                        "Branch ownership cannot be reassigned.");
                }
            }

            if (entry.Entity.BranchId != activeBranchId)
            {
                throw new TenantAccessException(
                    "branch.cross_branch_write",
                    "A branch-owned entity cannot be modified or deleted from another branch.");
            }
        }
    }

    private static bool IsChanged(EntityEntry entry) =>
        entry.State is not EntityState.Detached
            and not EntityState.Unchanged;
}
