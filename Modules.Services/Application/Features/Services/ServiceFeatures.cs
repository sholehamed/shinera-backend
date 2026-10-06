using Modules.System.Identity.Application.Authorization;
using Modules.System.Services.Application.Abstractions;
using Modules.System.Services.Application.Authorization;
using ServiceEntity = Modules.System.Services.Domain.Entities.Service;

namespace Modules.System.Services.Application.Features.Services;

public sealed record ServiceListItemDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    int DurationMinutes,
    decimal Price,
    bool IsActive);

public sealed record ServiceDetailsDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    bool IsActive);

public sealed record ServiceListQuery
    : IQuery<PagedList<ServiceListItemDto>>
{
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public bool? IsActive { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class ServiceListQueryValidator
    : AbstractValidator<ServiceListQuery>
{
    public ServiceListQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.Search)
            .MaximumLength(200);
    }
}

public sealed class ServiceListQueryHandler(
    IServiceCatalogDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : IQueryHandler<
        ServiceListQuery,
        PagedList<ServiceListItemDto>>
{
    public async Task<PagedList<ServiceListItemDto>> Handle(
        ServiceListQuery query,
        CancellationToken cancellationToken)
    {
        await ServiceCatalogPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Services.View,
            cancellationToken);

        var services = db.Services
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            services = services.Where(x =>
                x.Name.Contains(search) ||
                (x.Description != null &&
                 x.Description.Contains(search)) ||
                x.Category.Name.Contains(search));
        }

        if (query.CategoryId.HasValue)
        {
            services = services.Where(
                x => x.CategoryId == query.CategoryId.Value);
        }

        if (query.IsActive.HasValue)
        {
            services = services.Where(
                x => x.IsActive == query.IsActive.Value);
        }

        var count = await services.CountAsync(
            cancellationToken);

        var items = await services
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new ServiceListItemDto(
                x.Id,
                x.CategoryId,
                x.Category.Name,
                x.Name,
                x.DurationMinutes,
                x.Price,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedList<ServiceListItemDto>(
            items,
            count,
            query.PageNumber,
            query.PageSize);
    }
}

public sealed record ServiceDetailsQuery(Guid Id)
    : IQuery<ServiceDetailsDto>;

public sealed class ServiceDetailsQueryHandler(
    IServiceCatalogDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : IQueryHandler<ServiceDetailsQuery, ServiceDetailsDto>
{
    public async Task<ServiceDetailsDto> Handle(
        ServiceDetailsQuery query,
        CancellationToken cancellationToken)
    {
        await ServiceCatalogPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Services.View,
            cancellationToken);

        var service = await db.Services
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new ServiceDetailsDto(
                x.Id,
                x.CategoryId,
                x.Category.Name,
                x.Name,
                x.Description,
                x.DurationMinutes,
                x.Price,
                x.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

        return Guard.Against.NotFound(
            query.Id,
            service);
    }
}

public sealed record CreateServiceCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    bool IsActive = true)
    : ICommand<Result<Guid>>;

public sealed class CreateServiceCommandValidator
    : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateServiceCommandHandler(
    IServiceCatalogDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : ICommandHandler<CreateServiceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateServiceCommand command,
        CancellationToken cancellationToken)
    {
        await ServiceCatalogPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Services.Create,
            cancellationToken);

        var tenantId = currentTenant.TenantId
            ?? throw new Application.SharedKernel.Exceptions.TenantAccessException(
                "tenant.context_missing",
                "A tenant context is required for service catalog operations.");

        var categoryExists = await db.ServiceCategories
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == command.CategoryId,
                cancellationToken);

        if (!categoryExists)
        {
            return Result<Guid>.Failure(
                Error.Validation(
                    "service.category_invalid",
                    "The selected service category is not valid for the current tenant."));
        }

        var service = new ServiceEntity(
            tenantId,
            command.CategoryId,
            command.Name,
            command.DurationMinutes,
            command.Price,
            command.Description,
            command.IsActive);

        db.Services.Add(service);
        await db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(service.Id);
    }
}

public sealed record UpdateServiceCommand(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    bool IsActive)
    : ICommand<Result>;

public sealed class UpdateServiceCommandValidator
    : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateServiceCommandHandler(
    IServiceCatalogDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : ICommandHandler<UpdateServiceCommand, Result>
{
    public async Task<Result> Handle(
        UpdateServiceCommand command,
        CancellationToken cancellationToken)
    {
        await ServiceCatalogPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Services.Update,
            cancellationToken);

        var service = await db.Services
            .SingleOrDefaultAsync(
                x => x.Id == command.Id,
                cancellationToken);

        Guard.Against.NotFound(command.Id, service);

        var categoryExists = await db.ServiceCategories
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == command.CategoryId,
                cancellationToken);

        if (!categoryExists)
        {
            return Result.Failure(
                Error.Validation(
                    "service.category_invalid",
                    "The selected service category is not valid for the current tenant."));
        }

        service.Update(
            command.CategoryId,
            command.Name,
            command.DurationMinutes,
            command.Price,
            command.Description,
            command.IsActive);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed record SetServiceActiveCommand(
    Guid Id,
    bool IsActive)
    : ICommand;

public sealed class SetServiceActiveCommandHandler(
    IServiceCatalogDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : ICommandHandler<SetServiceActiveCommand>
{
    public async Task Handle(
        SetServiceActiveCommand command,
        CancellationToken cancellationToken)
    {
        await ServiceCatalogPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Services.Update,
            cancellationToken);

        var service = await db.Services
            .SingleOrDefaultAsync(
                x => x.Id == command.Id,
                cancellationToken);

        Guard.Against.NotFound(command.Id, service);

        service.SetActive(command.IsActive);
        await db.SaveChangesAsync(cancellationToken);
    }
}
