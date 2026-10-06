using Application.SharedKernel.Exceptions;
using Modules.System.Identity.Application.Abstractions;

namespace Modules.System.Identity.Application.Features.Workspace.Queries;

public sealed record CurrentWorkspaceQuery
    : IQuery<CurrentWorkspaceDto>;

public sealed record CurrentWorkspaceDto(
    Guid UserId,
    Guid? ActiveTenantId,
    Guid? ActiveBranchId,
    IReadOnlyList<WorkspaceTenantDto> Tenants);

public sealed record WorkspaceTenantDto(
    Guid Id,
    string Name,
    string Slug,
    IReadOnlyList<WorkspaceBranchDto> Branches);

public sealed record WorkspaceBranchDto(
    Guid Id,
    string Name,
    bool IsMain);

public sealed class CurrentWorkspaceQueryHandler(
    IIdentityDbContext db,
    ITenantContext tenantContext)
    : IQueryHandler<CurrentWorkspaceQuery, CurrentWorkspaceDto>
{
    public async Task<CurrentWorkspaceDto> Handle(
        CurrentWorkspaceQuery request,
        CancellationToken cancellationToken)
    {
        var userId = tenantContext.UserId
            ?? throw new TenantAccessException(
                "tenant.user_context_invalid",
                "The authenticated user context is invalid.");

        var tenantIds = tenantContext.ReadableTenantIds.ToArray();

        using (tenantContext.DisableFilter())
        {
            var tenants = await db.Tenants
                .AsNoTracking()
                .Where(x =>
                    tenantIds.Contains(x.Id) &&
                    x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Slug
                })
                .ToListAsync(cancellationToken);

            var branchMemberships = await db.BranchMemberships
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.IsActive &&
                    tenantIds.Contains(x.TenantId) &&
                    x.Branch.TenantId == x.TenantId &&
                    x.Branch.IsActive)
                .Select(x => new
                {
                    x.TenantId,
                    x.Branch.Id,
                    x.Branch.Name,
                    x.Branch.IsMain
                })
                .ToListAsync(cancellationToken);

            var tenantDtos = tenants
                .Select(tenant => new WorkspaceTenantDto(
                    tenant.Id,
                    tenant.Name,
                    tenant.Slug,
                    branchMemberships
                        .Where(x => x.TenantId == tenant.Id)
                        .OrderByDescending(x => x.IsMain)
                        .ThenBy(x => x.Name)
                        .Select(x => new WorkspaceBranchDto(
                            x.Id,
                            x.Name,
                            x.IsMain))
                        .ToArray()))
                .ToArray();

            return new CurrentWorkspaceDto(
                userId,
                tenantContext.ActiveTenantId,
                tenantContext.ActiveBranchId,
                tenantDtos);
        }
    }
}
