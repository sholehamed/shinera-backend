using Microsoft.EntityFrameworkCore;
using Modules.System.Identity.Application.Abstractions;

namespace Modules.System.Identity.Application.Services;

public sealed class TenantAccessResolver(
    IIdentityDbContext db) : ITenantAccessResolver
{
    public async Task<(Guid[] Readable, Guid[] Writable)> ResolveAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Ordinary workspace access is membership-based only.
        // Platform/system administration must use an explicit privileged
        // system context rather than bypassing membership via token roles.
        var tenantIds = await db.TenantMemberships
            .IgnoreQueryFilters(["tenant"])
            .AsNoTracking()
            .Where(membership =>
                membership.UserId == userId &&
                membership.IsActive &&
                membership.Tenant.IsActive)
            .Select(membership => membership.TenantId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        Array.Sort(tenantIds);

        // Membership establishes workspace access. Action-level write authorization
        // is evaluated separately by the permission system.
        return (tenantIds, tenantIds);
    }
}
