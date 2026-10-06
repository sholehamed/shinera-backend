using Modules.System.Identity.Application.Abstractions;

namespace Modules.System.Identity.Application.Features.Branches.Queries;

public sealed record BranchListQuery
    : IQuery<IReadOnlyList<BranchListItemDto>>;

public sealed record BranchListItemDto(
    Guid Id,
    string Name,
    string? Phone,
    string? Address,
    bool IsMain,
    bool IsActive);

public sealed class BranchListQueryHandler(
    IIdentityDbContext db)
    : IQueryHandler<BranchListQuery, IReadOnlyList<BranchListItemDto>>
{
    public async Task<IReadOnlyList<BranchListItemDto>> Handle(
        BranchListQuery request,
        CancellationToken cancellationToken)
    {
        return await db.Branches
            .AsNoTracking()
            .OrderByDescending(x => x.IsMain)
            .ThenBy(x => x.Name)
            .Select(x => new BranchListItemDto(
                x.Id,
                x.Name,
                x.Phone,
                x.Address,
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
    bool IsMain,
    bool IsActive);

public sealed class BranchGetByIdQueryHandler(
    IIdentityDbContext db)
    : IQueryHandler<BranchGetByIdQuery, BranchDetailsDto>
{
    public async Task<BranchDetailsDto> Handle(
        BranchGetByIdQuery request,
        CancellationToken cancellationToken)
    {
        var branch = await db.Branches
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new BranchDetailsDto(
                x.Id,
                x.Name,
                x.Phone,
                x.Address,
                x.IsMain,
                x.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

        return Guard.Against.NotFound(request.Id, branch);
    }
}
