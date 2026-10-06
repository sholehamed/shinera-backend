using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Application.Features.Tenants.Commands;

public record TenantCreateCommand : MapTo<Tenant>, ICommand<Guid>
{
    public string Name { get; init; } = default!;
    public string? Domain { get; set; }
    public Guid? Logo { get; set; }
    public Guid? Favicon { get; set; }
    public string Slug { get; init; } = default!;
    public bool IsActive { get; init; } = true;
}

public sealed class TenantCreateCommandValidator
    : AbstractValidator<TenantCreateCommand>
{
    public TenantCreateCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100);
    }
}

public sealed class TenantCreateCommandHandler(
    IIdentityDbContext dbContext,
    IMapper mapper,
    ITenantContext tenantContext)
    : ICommandHandler<TenantCreateCommand, Guid>
{
    public async Task<Guid> Handle(
        TenantCreateCommand command,
        CancellationToken cancellationToken)
    {
        var tenant = mapper.Map<Tenant>(command);

        var mainBranch = new Branch(
            tenant.Id,
            command.Name.Trim(),
            phone: null,
            address: null,
            isMain: true);

        using (tenantContext.DisableFilter())
        {
            await dbContext.Tenants.AddAsync(
                tenant,
                cancellationToken);

            await dbContext.Branches.AddAsync(
                mainBranch,
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return tenant.Id;
    }
}
