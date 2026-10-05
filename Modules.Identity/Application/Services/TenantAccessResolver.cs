using Microsoft.EntityFrameworkCore;
using Modules.System.Identity.Application.Abstractions;

namespace Modules.System.Identity.Application.Services;

public sealed class TenantAccessResolver(
    IIdentityDbContext db) : ITenantAccessResolver
{
    public async Task<(Guid[] Readable, Guid[] Writable)> ResolveAsync(
        Guid userId,
        bool isSuperAdmin,
        CancellationToken cancellationToken)
    {
        Guid[] tenantIds;

        if (isSuperAdmin)
        {
            tenantIds = await db.Tenants
                .AsNoTracking()
                .Where(tenant => tenant.IsActive)
                .Select(tenant => tenant.Id)
                .ToArrayAsync(cancellationToken);
        }
        else
        {
            // Resolving workspace membership happens before an active Tenant exists,
            // so bypassing only the named Tenant filter is intentional here.
            tenantIds = await db.TenantMemberships
                .IgnoreQueryFilters(["tenant"])
                .AsNoTracking()
                .Where(membership =>
                    membership.UserId == userId &&
                    membership.IsActive &&
                    membership.Tenant.IsActive)
                .Select(membership => membership.TenantId)
                .Distinct()
                .ToArrayAsync(cancellationToken);
        }

        Array.Sort(tenantIds);

        // Membership establishes workspace access. Action-level write authorization
        // is evaluated separately by the permission system.
        return (tenantIds, tenantIds);
    }
}
