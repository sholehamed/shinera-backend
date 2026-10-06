using Microsoft.EntityFrameworkCore;
using Modules.System.Identity.Application.Abstractions;

namespace Modules.System.Identity.Application.Services;

public sealed class BranchAccessResolver(
    IIdentityDbContext db) : IBranchAccessResolver
{
    public async Task<(Guid[] Readable, Guid[] Writable)> ResolveAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var branchIds = await db.BranchMemberships
            .IgnoreQueryFilters(["tenant"])
            .AsNoTracking()
            .Where(membership =>
                membership.TenantId == tenantId &&
                membership.UserId == userId &&
                membership.IsActive &&
                membership.Branch.TenantId == tenantId &&
                membership.Branch.IsActive)
            .Select(membership => membership.BranchId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        Array.Sort(branchIds);

        return (branchIds, branchIds);
    }
}
