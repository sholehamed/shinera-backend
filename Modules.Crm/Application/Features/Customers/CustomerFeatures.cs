using Modules.System.Crm.Application.Abstractions;
using Modules.System.Crm.Application.Authorization;
using Modules.System.Crm.Application.Normalization;
using Modules.System.Crm.Domain.Entities;
using Modules.System.Identity.Application.Authorization;

namespace Modules.System.Crm.Application.Features.Customers;

public sealed record CustomerListItemDto(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    string Mobile,
    string? Email,
    bool IsVip,
    bool IsActive);

public sealed record CustomerNoteDto(
    Guid Id,
    string Content,
    DateTimeOffset CreatedAt,
    Guid CreatedBy);

public sealed record CustomerDetailsDto(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    string Mobile,
    string? Email,
    DateOnly? Birthday,
    string? Gender,
    string? Notes,
    bool IsVip,
    bool IsActive,
    IReadOnlyList<CustomerNoteDto> NoteEntries);

public sealed record CustomerSearchQuery
    : IQuery<PagedList<CustomerListItemDto>>
{
    public string? Search { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class CustomerSearchQueryValidator
    : AbstractValidator<CustomerSearchQuery>
{
    public CustomerSearchQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.Search)
            .MaximumLength(200);
    }
}

public sealed class CustomerSearchQueryHandler(
    ICrmDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : IQueryHandler<
        CustomerSearchQuery,
        PagedList<CustomerListItemDto>>
{
    public async Task<PagedList<CustomerListItemDto>> Handle(
        CustomerSearchQuery query,
        CancellationToken cancellationToken)
    {
        await CrmPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Customers.View,
            cancellationToken);

        var customers = db.Customers
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            var normalizedMobile =
                CustomerMobileNormalizer.Normalize(search);

            customers = customers.Where(x =>
                x.FirstName.Contains(search) ||
                x.LastName.Contains(search) ||
                x.Mobile.Contains(search) ||
                (x.Email != null &&
                    x.Email.Contains(search)) ||
                (normalizedMobile.Length > 0 &&
                    x.NormalizedMobile.Contains(
                        normalizedMobile)));
        }

        var count = await customers.CountAsync(
            cancellationToken);

        var items = await customers
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ThenBy(x => x.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new CustomerListItemDto(
                x.Id,
                x.UserId,
                x.FirstName,
                x.LastName,
                x.Mobile,
                x.Email,
                x.IsVip,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedList<CustomerListItemDto>(
            items,
            count,
            query.PageNumber,
            query.PageSize);
    }
}

public sealed record CustomerDetailsQuery(Guid Id)
    : IQuery<CustomerDetailsDto>;

public sealed class CustomerDetailsQueryHandler(
    ICrmDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : IQueryHandler<
        CustomerDetailsQuery,
        CustomerDetailsDto>
{
    public async Task<CustomerDetailsDto> Handle(
        CustomerDetailsQuery query,
        CancellationToken cancellationToken)
    {
        await CrmPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Customers.View,
            cancellationToken);

        var customer = await db.Customers
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new CustomerDetailsDto(
                x.Id,
                x.UserId,
                x.FirstName,
                x.LastName,
                x.Mobile,
                x.Email,
                x.Birthday,
                x.Gender,
                x.Notes,
                x.IsVip,
                x.IsActive,
                x.NoteEntries
                    .OrderByDescending(note =>
                        note.CreatedAt)
                    .ThenByDescending(note =>
                        note.Id)
                    .Select(note =>
                        new CustomerNoteDto(
                            note.Id,
                            note.Content,
                            note.CreatedAt,
                            note.CreatedBy))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(
            query.Id,
            customer);

        return customer;
    }
}

public sealed record CreateCustomerCommand(
    string FirstName,
    string LastName,
    string Mobile,
    string? Email,
    DateOnly? Birthday,
    string? Gender,
    string? Notes,
    bool IsVip)
    : ICommand<Result<Guid>>;

public sealed class CreateCustomerCommandValidator
    : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Mobile)
            .NotEmpty()
            .MaximumLength(32)
            .Must(value =>
                !string.IsNullOrWhiteSpace(value) &&
                CustomerMobileNormalizer.Normalize(value)
                    .Length > 0)
            .WithMessage(
                "Mobile must contain at least one digit.");

        RuleFor(x => x.Email)
            .MaximumLength(256)
            .EmailAddress()
            .When(x =>
                !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Gender)
            .MaximumLength(32);

        RuleFor(x => x.Notes)
            .MaximumLength(2000);
    }
}

public sealed class CreateCustomerCommandHandler(
    ICrmDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : ICommandHandler<
        CreateCustomerCommand,
        Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        await CrmPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Customers.Create,
            cancellationToken);

        var tenantId = currentTenant.TenantId
            ?? throw new TenantAccessException(
                "tenant.context_missing",
                "A tenant context is required for CRM operations.");

        var normalizedMobile =
            CustomerMobileNormalizer.Normalize(
                command.Mobile);

        var duplicateExists = await db.Customers
            .AsNoTracking()
            .AnyAsync(
                x => x.NormalizedMobile ==
                    normalizedMobile,
                cancellationToken);

        if (duplicateExists)
        {
            return Result.Failure<Guid>(
                Error.Conflict(
                    "customer.mobile_duplicate",
                    "A customer with this mobile already exists in the current tenant."));
        }

        var customer = new Customer(
            tenantId,
            command.FirstName,
            command.LastName,
            command.Mobile,
            normalizedMobile,
            command.Email,
            command.Birthday,
            command.Gender,
            command.Notes,
            command.IsVip);

        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(customer.Id);
    }
}

public sealed record AddCustomerNoteCommand(
    Guid CustomerId,
    string Content)
    : ICommand<Result<Guid>>;

public sealed class AddCustomerNoteCommandValidator
    : AbstractValidator<AddCustomerNoteCommand>
{
    public AddCustomerNoteCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty();

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(2000);
    }
}

public sealed class AddCustomerNoteCommandHandler(
    ICrmDbContext db,
    ICurrentTenant currentTenant,
    IPermissionAuthorizationService authorizationService)
    : ICommandHandler<
        AddCustomerNoteCommand,
        Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        AddCustomerNoteCommand command,
        CancellationToken cancellationToken)
    {
        await CrmPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            currentTenant,
            SystemPermissionCatalog.Customers.AddNote,
            cancellationToken);

        var tenantId = currentTenant.TenantId
            ?? throw new TenantAccessException(
                "tenant.context_missing",
                "A tenant context is required for CRM operations.");

        var customerExists = await db.Customers
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == command.CustomerId,
                cancellationToken);

        if (!customerExists)
        {
            return Result.Failure<Guid>(
                Error.NotFound(
                    "customer.not_found",
                    "Customer was not found in the current tenant."));
        }

        var note = new CustomerNote(
            tenantId,
            command.CustomerId,
            command.Content);

        db.CustomerNotes.Add(note);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(note.Id);
    }
}
