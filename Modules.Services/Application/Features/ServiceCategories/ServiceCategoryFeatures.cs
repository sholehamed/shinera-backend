using Modules.System.Identity.Application.Authorization;
using Modules.System.Services.Application.Abstractions;
using Modules.System.Services.Application.Authorization;
using Modules.System.Services.Domain.Entities;

namespace Modules.System.Services.Application.Features.ServiceCategories;

public sealed record ServiceCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    bool IsActive);

public sealed record ServiceCategoryListQuery(
    bool? IsActive = null)
    : IQuery<IReadOnlyList<ServiceCategoryDto>>;

public sealed class ServiceCategoryListQueryHandler(
    IServiceCatalogDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : IQueryHandler<
        ServiceCategoryListQuery,
        IReadOnlyList<ServiceCategoryDto>>
{
    public async Task<IReadOnlyList<ServiceCategoryDto>> Handle(
        ServiceCategoryListQuery query,
        CancellationToken cancellationToken)
    {
        await ServiceCatalogPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Services.View,
            cancellationToken);

        var categories = db.ServiceCategories
            .AsNoTracking()
            .AsQueryable();

        if (query.IsActive.HasValue)
        {
            categories = categories.Where(
                x => x.IsActive == query.IsActive.Value);
        }

        return await categories
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ServiceCategoryDto(
                x.Id,
                x.Name,
                x.Description,
                x.SortOrder,
                x.IsActive))
            .ToListAsync(cancellationToken);
    }
}

public sealed record ServiceCategoryDetailsQuery(Guid Id)
    : IQuery<ServiceCategoryDto>;

public sealed class ServiceCategoryDetailsQueryHandler(
    IServiceCatalogDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : IQueryHandler<
        ServiceCategoryDetailsQuery,
        ServiceCategoryDto>
{
    public async Task<ServiceCategoryDto> Handle(
        ServiceCategoryDetailsQuery query,
        CancellationToken cancellationToken)
    {
        await ServiceCatalogPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Services.View,
            cancellationToken);

        var category = await db.ServiceCategories
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new ServiceCategoryDto(
                x.Id,
                x.Name,
                x.Description,
                x.SortOrder,
                x.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

        return Guard.Against.NotFound(
            query.Id,
            category);
    }
}

public sealed record CreateServiceCategoryCommand(
    string Name,
    string? Description,
    int SortOrder = 0,
    bool IsActive = true)
    : ICommand<Guid>;

public sealed class CreateServiceCategoryCommandValidator
    : AbstractValidator<CreateServiceCategoryCommand>
{
    public CreateServiceCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateServiceCategoryCommandHandler(
    IServiceCatalogDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : ICommandHandler<CreateServiceCategoryCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateServiceCategoryCommand command,
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

        var category = new ServiceCategory(
            tenantId,
            command.Name,
            command.Description,
            command.SortOrder,
            command.IsActive);

        db.ServiceCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}

public sealed record UpdateServiceCategoryCommand(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    bool IsActive)
    : ICommand;

public sealed class UpdateServiceCategoryCommandValidator
    : AbstractValidator<UpdateServiceCategoryCommand>
{
    public UpdateServiceCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateServiceCategoryCommandHandler(
    IServiceCatalogDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : ICommandHandler<UpdateServiceCategoryCommand>
{
    public async Task Handle(
        UpdateServiceCategoryCommand command,
        CancellationToken cancellationToken)
    {
        await ServiceCatalogPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Services.Update,
            cancellationToken);

        var category = await db.ServiceCategories
            .SingleOrDefaultAsync(
                x => x.Id == command.Id,
                cancellationToken);

        Guard.Against.NotFound(command.Id, category);

        category.Update(
            command.Name,
            command.Description,
            command.SortOrder,
            command.IsActive);

        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record DeleteServiceCategoryCommand(Guid Id)
    : ICommand<Result>;

public sealed class DeleteServiceCategoryCommandHandler(
    IServiceCatalogDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : ICommandHandler<DeleteServiceCategoryCommand, Result>
{
    public async Task<Result> Handle(
        DeleteServiceCategoryCommand command,
        CancellationToken cancellationToken)
    {
        await ServiceCatalogPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Services.Delete,
            cancellationToken);

        var category = await db.ServiceCategories
            .SingleOrDefaultAsync(
                x => x.Id == command.Id,
                cancellationToken);

        Guard.Against.NotFound(command.Id, category);

        var hasServices = await db.Services
            .AsNoTracking()
            .AnyAsync(
                x => x.CategoryId == command.Id,
                cancellationToken);

        if (hasServices)
        {
            return Result.Failure(
                Error.Conflict(
                    "service_category.in_use",
                    "A category that contains services cannot be deleted."));
        }

        db.ServiceCategories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
