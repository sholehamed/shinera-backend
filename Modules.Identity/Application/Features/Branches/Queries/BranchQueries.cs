using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Application.Features.Branches;
using Modules.System.Identity.Application.Authorization;

namespace Modules.System.Identity.Application.Features.Branches.Queries;

public sealed record BranchListQuery
    : IQuery<IReadOnlyList<BranchListItemDto>>;

public sealed record BranchListItemDto(
    Guid Id,
    string Name,
    string? Phone,
    string? Address,
    string TimeZoneId,
    bool IsMain,
    bool IsActive);

public sealed class BranchListQueryHandler(
    IIdentityDbContext db,
    ITenantContext tenantContext,
    IPermissionAuthorizationService authorizationService)
    : IQueryHandler<BranchListQuery, IReadOnlyList<BranchListItemDto>>
{
    public async Task<IReadOnlyList<BranchListItemDto>> Handle(
        BranchListQuery request,
        CancellationToken cancellationToken)
    {
        var visibleBranchIds =
            await BranchPermissionGuard.ResolveVisibleBranchIdsAsync(
                authorizationService,
                tenantContext,
                SystemPermissionCatalog.Branches.List,
                cancellationToken);

        var query = db.Branches.AsNoTracking();

        if (visibleBranchIds is not null)
        {
            query = query.Where(
                x => visibleBranchIds.Contains(x.Id));
        }

        return await query
            .OrderByDescending(x => x.IsMain)
            .ThenBy(x => x.Name)
            .Select(x => new BranchListItemDto(
                x.Id,
                x.Name,
                x.Phone,
                x.Address,
                x.TimeZoneId,
                x.IsMain,
                x.IsActive))
            .ToListAsync(cancellationToken);
    }
}

public sealed record BranchGetByIdQuery(Guid Id)
    : IQuery<BranchDetailsDto>;

public sealed record BranchDetailsDto(
    Guid Id,
    string Name,
    string? Phone,
    string? Address,
    string TimeZoneId,
    bool IsMain,
    bool IsActive);

public sealed class BranchGetByIdQueryHandler(
    IIdentityDbContext db,
    ITenantContext tenantContext,
    IPermissionAuthorizationService authorizationService)
    : IQueryHandler<BranchGetByIdQuery, BranchDetailsDto>
{
    public async Task<BranchDetailsDto> Handle(
        BranchGetByIdQuery request,
        CancellationToken cancellationToken)
    {
        await BranchPermissionGuard.RequireBranchScopeAsync(
            authorizationService,
            tenantContext,
            SystemPermissionCatalog.Branches.List,
            request.Id,
            requireWriteAccess: false,
            cancellationToken);

        var branch = await db.Branches
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new BranchDetailsDto(
                x.Id,
                x.Name,
                x.Phone,
                x.Address,
                x.TimeZoneId,
                x.IsMain,
                x.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

        return Guard.Against.NotFound(request.Id, branch);
    }
}
