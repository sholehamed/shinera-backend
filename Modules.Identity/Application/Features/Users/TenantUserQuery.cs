using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Application.Features.Users;

internal static class TenantUserQuery
{
    public static IQueryable<User> CurrentTenantUsers(
        this IIdentityDbContext dbContext,
        bool activeMembershipOnly = true)
    {
        var memberships = dbContext.TenantMemberships.AsQueryable();

        if (activeMembershipOnly)
            memberships = memberships.Where(x => x.IsActive);

        return memberships
            .Select(x => x.User)
            .Distinct();
    }
}
