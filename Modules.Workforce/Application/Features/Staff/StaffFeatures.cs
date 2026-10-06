using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Services.Application.Abstractions;
using Modules.System.Workforce.Application.Abstractions;
using Modules.System.Workforce.Application.Authorization;
using Modules.System.Workforce.Domain.Entities;
using ServiceEntity = Modules.System.Services.Domain.Entities.Service;
using StaffEntity = Modules.System.Workforce.Domain.Entities.Staff;

namespace Modules.System.Workforce.Application.Features.Staff;

public sealed record StaffListItemDto(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    bool IsActive,
    int BranchCount,
    int ServiceCount);

public sealed record StaffBranchDto(
    Guid Id,
    string Name,
    bool IsActive);

public sealed record StaffServiceDto(
    Guid Id,
    string Name,
    bool IsActive);

public sealed record StaffDetailsDto(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    bool IsActive,
    IReadOnlyList<StaffBranchDto> Branches,
    IReadOnlyList<StaffServiceDto> Services);

public sealed record StaffListQuery
    : IQuery<PagedList<StaffListItemDto>>
{
    public string? Search { get; init; }
    public Guid? BranchId { get; init; }
    public Guid? ServiceId { get; init; }
    public bool? IsActive { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class StaffListQueryValidator
    : AbstractValidator<StaffListQuery>
{
    public StaffListQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.Search)
            .MaximumLength(200);
    }
}

public sealed class StaffListQueryHandler(
    IWorkforceDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : IQueryHandler<
        StaffListQuery,
        PagedList<StaffListItemDto>>
{
    public async Task<PagedList<StaffListItemDto>> Handle(
        StaffListQuery query,
        CancellationToken cancellationToken)
    {
        await WorkforcePermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Staff.View,
            cancellationToken);

        var staff = db.Staff
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            staff = staff.Where(x =>
                x.FirstName.Contains(search) ||
                x.LastName.Contains(search) ||
                x.Phone.Contains(search) ||
                x.Email.Contains(search));
        }

        if (query.BranchId.HasValue)
        {
            staff = staff.Where(x =>
                x.Branches.Any(link =>
                    link.BranchId == query.BranchId.Value));
        }

        if (query.ServiceId.HasValue)
        {
            staff = staff.Where(x =>
                x.Services.Any(link =>
                    link.ServiceId == query.ServiceId.Value));
        }

        if (query.IsActive.HasValue)
        {
            staff = staff.Where(
                x => x.IsActive == query.IsActive.Value);
        }

        var count = await staff.CountAsync(
            cancellationToken);

        var items = await staff
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ThenBy(x => x.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new StaffListItemDto(
                x.Id,
                x.UserId,
                x.FirstName,
                x.LastName,
                x.Phone,
                x.Email,
                x.IsActive,
                x.Branches.Count,
                x.Services.Count))
            .ToListAsync(cancellationToken);

        return new PagedList<StaffListItemDto>(
            items,
            count,
            query.PageNumber,
            query.PageSize);
    }
}

public sealed record StaffDetailsQuery(Guid Id)
    : IQuery<StaffDetailsDto>;

public sealed class StaffDetailsQueryHandler(
    IWorkforceDbContext db,
    IIdentityDbContext identityDb,
    IServiceCatalogDbContext servicesDb,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : IQueryHandler<StaffDetailsQuery, StaffDetailsDto>
{
    public async Task<StaffDetailsDto> Handle(
        StaffDetailsQuery query,
        CancellationToken cancellationToken)
    {
        await WorkforcePermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Staff.View,
            cancellationToken);

        var staff = await db.Staff
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.FirstName,
                x.LastName,
                x.Phone,
                x.Email,
                x.IsActive,
                BranchIds = x.Branches
                    .Select(link => link.BranchId)
                    .ToArray(),
                ServiceIds = x.Services
                    .Select(link => link.ServiceId)
                    .ToArray()
            })
            .SingleOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(query.Id, staff);

        var branches = await identityDb.Branches
            .AsNoTracking()
            .Where(x => staff.BranchIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new StaffBranchDto(
                x.Id,
                x.Name,
                x.IsActive))
            .ToListAsync(cancellationToken);

        var services = await servicesDb.Services
            .AsNoTracking()
            .Where(x => staff.ServiceIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new StaffServiceDto(
                x.Id,
                x.Name,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return new StaffDetailsDto(
            staff.Id,
            staff.UserId,
            staff.FirstName,
            staff.LastName,
            staff.Phone,
            staff.Email,
            staff.IsActive,
            branches,
            services);
    }
}

public sealed record CreateStaffCommand(
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    bool IsActive = true)
    : ICommand<Guid>;

public sealed class CreateStaffCommandValidator
    : AbstractValidator<CreateStaffCommand>
{
    public CreateStaffCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}

public sealed class CreateStaffCommandHandler(
    IWorkforceDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : ICommandHandler<CreateStaffCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateStaffCommand command,
        CancellationToken cancellationToken)
    {
        await WorkforcePermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Staff.Create,
            cancellationToken);

        var tenantId = currentTenant.TenantId
            ?? throw new TenantAccessException(
                "tenant.context_missing",
                "A tenant context is required for workforce operations.");

        var staff = new StaffEntity(
            tenantId,
            command.FirstName,
            command.LastName,
            command.Phone,
            command.Email,
            command.IsActive);

        db.Staff.Add(staff);
        await db.SaveChangesAsync(cancellationToken);

        return staff.Id;
    }
}

public sealed record UpdateStaffCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    bool IsActive)
    : ICommand;

public sealed class UpdateStaffCommandValidator
    : AbstractValidator<UpdateStaffCommand>
{
    public UpdateStaffCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}

public sealed class UpdateStaffCommandHandler(
    IWorkforceDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService,
    IStaffBookingConcurrencyGuard concurrencyGuard)
    : ICommandHandler<UpdateStaffCommand>
{
    public async Task Handle(
        UpdateStaffCommand command,
        CancellationToken cancellationToken)
    {
        await WorkforcePermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Staff.Update,
            cancellationToken);

        var tenantId = currentTenant.TenantId
            ?? throw new TenantAccessException(
                "tenant.context_missing",
                "A tenant context is required for workforce operations.");

        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        var staffLocked = await concurrencyGuard.TryAcquireAsync(
            db.Database,
            tenantId,
            command.Id,
            cancellationToken);

        if (!staffLocked)
            throw new KeyNotFoundException("Staff not found.");

        var staff = await db.Staff
            .SingleOrDefaultAsync(
                x => x.Id == command.Id,
                cancellationToken);

        Guard.Against.NotFound(command.Id, staff);

        staff.Update(
            command.FirstName,
            command.LastName,
            command.Phone,
            command.Email,
            command.IsActive);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

public sealed record SetStaffActiveCommand(
    Guid Id,
    bool IsActive)
    : ICommand;

public sealed class SetStaffActiveCommandHandler(
    IWorkforceDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService,
    IStaffBookingConcurrencyGuard concurrencyGuard)
    : ICommandHandler<SetStaffActiveCommand>
{
    public async Task Handle(
        SetStaffActiveCommand command,
        CancellationToken cancellationToken)
    {
        await WorkforcePermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Staff.Update,
            cancellationToken);

        var tenantId = currentTenant.TenantId
            ?? throw new TenantAccessException(
                "tenant.context_missing",
                "A tenant context is required for workforce operations.");

        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        var staffLocked = await concurrencyGuard.TryAcquireAsync(
            db.Database,
            tenantId,
            command.Id,
            cancellationToken);

        if (!staffLocked)
            throw new KeyNotFoundException("Staff not found.");

        var staff = await db.Staff
            .SingleOrDefaultAsync(
                x => x.Id == command.Id,
                cancellationToken);

        Guard.Against.NotFound(command.Id, staff);

        staff.SetActive(command.IsActive);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

public sealed record ReplaceStaffBranchesCommand(
    Guid StaffId,
    IReadOnlyCollection<Guid> BranchIds)
    : ICommand<Result>;

public sealed class ReplaceStaffBranchesCommandValidator
    : AbstractValidator<ReplaceStaffBranchesCommand>
{
    public ReplaceStaffBranchesCommandValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty();
        RuleFor(x => x.BranchIds).NotNull();
        RuleForEach(x => x.BranchIds).NotEmpty();
    }
}

public sealed class ReplaceStaffBranchesCommandHandler(
    IWorkforceDbContext db,
    IIdentityDbContext identityDb,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService,
    IStaffBookingConcurrencyGuard concurrencyGuard)
    : ICommandHandler<ReplaceStaffBranchesCommand, Result>
{
    public async Task<Result> Handle(
        ReplaceStaffBranchesCommand command,
        CancellationToken cancellationToken)
    {
        await WorkforcePermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Staff.AssignBranches,
            cancellationToken);

        var tenantId = currentTenant.TenantId
            ?? throw new TenantAccessException(
                "tenant.context_missing",
                "A tenant context is required for workforce operations.");

        var requested = command.BranchIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        var validIds = await identityDb.Branches
            .AsNoTracking()
            .Where(x => requested.Contains(x.Id))
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);

        if (validIds.Length != requested.Length)
        {
            return Result.Failure(
                Error.Validation(
                    "staff.branch_invalid",
                    "One or more selected branches are not valid for the current tenant."));
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        var staffLocked = await concurrencyGuard.TryAcquireAsync(
            db.Database,
            tenantId,
            command.StaffId,
            cancellationToken);

        if (!staffLocked)
            throw new KeyNotFoundException("Staff not found.");

        var staff = await db.Staff
            .SingleAsync(
                x => x.Id == command.StaffId,
                cancellationToken);

        var existing = await db.StaffBranches
            .Where(x => x.StaffId == command.StaffId)
            .ToListAsync(cancellationToken);

        var requestedSet = requested.ToHashSet();
        var existingSet = existing
            .Select(x => x.BranchId)
            .ToHashSet();

        db.StaffBranches.RemoveRange(
            existing.Where(x =>
                !requestedSet.Contains(x.BranchId)));

        db.StaffBranches.AddRange(
            requested
                .Where(x => !existingSet.Contains(x))
                .Select(branchId =>
                    new StaffBranch(
                        tenantId,
                        staff.Id,
                        branchId)));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed record ReplaceStaffServicesCommand(
    Guid StaffId,
    IReadOnlyCollection<Guid> ServiceIds)
    : ICommand<Result>;

public sealed class ReplaceStaffServicesCommandValidator
    : AbstractValidator<ReplaceStaffServicesCommand>
{
    public ReplaceStaffServicesCommandValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty();
        RuleFor(x => x.ServiceIds).NotNull();
        RuleForEach(x => x.ServiceIds).NotEmpty();
    }
}

public sealed class ReplaceStaffServicesCommandHandler(
    IWorkforceDbContext db,
    IServiceCatalogDbContext servicesDb,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService,
    IStaffBookingConcurrencyGuard concurrencyGuard)
    : ICommandHandler<ReplaceStaffServicesCommand, Result>
{
    public async Task<Result> Handle(
        ReplaceStaffServicesCommand command,
        CancellationToken cancellationToken)
    {
        await WorkforcePermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Staff.AssignServices,
            cancellationToken);

        var tenantId = currentTenant.TenantId
            ?? throw new TenantAccessException(
                "tenant.context_missing",
                "A tenant context is required for workforce operations.");

        var requested = command.ServiceIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        var validIds = await servicesDb.Services
            .AsNoTracking()
            .Where(x => requested.Contains(x.Id))
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);

        if (validIds.Length != requested.Length)
        {
            return Result.Failure(
                Error.Validation(
                    "staff.service_invalid",
                    "One or more selected services are not valid for the current tenant."));
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        var staffLocked = await concurrencyGuard.TryAcquireAsync(
            db.Database,
            tenantId,
            command.StaffId,
            cancellationToken);

        if (!staffLocked)
            throw new KeyNotFoundException("Staff not found.");

        var staff = await db.Staff
            .SingleAsync(
                x => x.Id == command.StaffId,
                cancellationToken);

        var existing = await db.StaffServices
            .Where(x => x.StaffId == command.StaffId)
            .ToListAsync(cancellationToken);

        var requestedSet = requested.ToHashSet();
        var existingSet = existing
            .Select(x => x.ServiceId)
            .ToHashSet();

        db.StaffServices.RemoveRange(
            existing.Where(x =>
                !requestedSet.Contains(x.ServiceId)));

        db.StaffServices.AddRange(
            requested
                .Where(x => !existingSet.Contains(x))
                .Select(serviceId =>
                    new StaffService(
                        tenantId,
                        staff.Id,
                        serviceId)));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
