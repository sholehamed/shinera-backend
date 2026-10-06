using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Application.Features.BusinessProfiles;

public sealed record BusinessProfileDto(
    Guid Id,
    Guid TenantId,
    string DisplayName,
    string BusinessType,
    BusinessMode Mode,
    string? Phone,
    string? Email,
    string? City,
    string? Address,
    Guid? LogoId,
    string? Description);

public sealed record CurrentBusinessProfileQuery
    : IQuery<BusinessProfileDto>;

public sealed class CurrentBusinessProfileQueryHandler(
    IIdentityDbContext db)
    : IQueryHandler<CurrentBusinessProfileQuery, BusinessProfileDto>
{
    public async Task<BusinessProfileDto> Handle(
        CurrentBusinessProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await db.BusinessProfiles
            .AsNoTracking()
            .Select(x => new BusinessProfileDto(
                x.Id,
                x.TenantId,
                x.DisplayName,
                x.BusinessType,
                x.Mode,
                x.Phone,
                x.Email,
                x.City,
                x.Address,
                x.LogoId,
                x.Description))
            .SingleOrDefaultAsync(cancellationToken);

        return Guard.Against.NotFound(
            "business-profile",
            profile);
    }
}

public sealed record UpdateBusinessProfileCommand(
    string DisplayName,
    string BusinessType,
    BusinessMode Mode,
    string? Phone,
    string? Email,
    string? City,
    string? Address,
    string? Description)
    : ICommand;

public sealed class UpdateBusinessProfileCommandValidator
    : AbstractValidator<UpdateBusinessProfileCommand>
{
    public UpdateBusinessProfileCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.BusinessType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Mode)
            .IsInEnum();

        RuleFor(x => x.Phone)
            .MaximumLength(32);

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .MaximumLength(256);

        RuleFor(x => x.City)
            .MaximumLength(150);

        RuleFor(x => x.Address)
            .MaximumLength(500);

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}

public sealed class UpdateBusinessProfileCommandHandler(
    IIdentityDbContext db)
    : ICommandHandler<UpdateBusinessProfileCommand>
{
    public async Task Handle(
        UpdateBusinessProfileCommand command,
        CancellationToken cancellationToken)
    {
        var profile = await db.BusinessProfiles
            .SingleOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(
            "business-profile",
            profile);

        profile.DisplayName = command.DisplayName.Trim();
        profile.BusinessType = command.BusinessType.Trim();
        profile.Mode = command.Mode;
        profile.Phone = Normalize(command.Phone);
        profile.Email = Normalize(command.Email);
        profile.City = Normalize(command.City);
        profile.Address = Normalize(command.Address);
        profile.Description = Normalize(command.Description);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
