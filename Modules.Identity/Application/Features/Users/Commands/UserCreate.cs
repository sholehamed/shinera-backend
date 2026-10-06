using Application.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Identity;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Application.Features.Users;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Application.Features.Users.Commands;

public record UserCreateCommand : MapTo<User>, ICommand<Guid>
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string Firstname { get; set; }
    public required string Lastname { get; set; }
    public required string Email { get; set; }
    public Guid? Avatar { get; set; }

    public override void Mapping(Profile profile)
    {
        profile.CreateMap<UserCreateCommand, User>()
            .ForMember(x => x.ImageId, opt => opt.MapFrom(x => x.Avatar))
            .ForMember(x => x.UserName, opt => opt.MapFrom(x => x.Username.Trim()))
            .ForMember(x => x.NormalizedEmail, opt => opt.MapFrom(x => x.Email.Trim().ToUpperInvariant()))
            .ForMember(x => x.NormalizedUserName, opt => opt.MapFrom(x => x.Username.Trim().ToUpperInvariant()));
    }
}

public sealed class UserCreateCommandValidator : AbstractValidator<UserCreateCommand>
{
    public UserCreateCommandValidator(IIdentityDbContext dbContext)
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MustAsync(async (username, cancellationToken) =>
                !await dbContext.CurrentTenantUsers().AnyAsync(
                    user => user.NormalizedUserName == username.Trim().ToUpperInvariant(),
                    cancellationToken))
            .WithMessage("Username is already in use.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(async (email, cancellationToken) =>
                !await dbContext.Users.AnyAsync(
                    user => user.NormalizedEmail == email.Trim().ToUpperInvariant(),
                    cancellationToken))
            .WithMessage("Email is already in use.");

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}

public sealed class UserCreateCommandHandler(
    IMapper mapper,
    IIdentityDbContext dbContext,
    ITenantContext tenantContext,
    IPasswordHasher<User> passwordHasher)
    : ICommandHandler<UserCreateCommand, Guid>
{
    public async Task<Guid> Handle(
        UserCreateCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.ActiveTenantId
            ?? throw new TenantAccessException(
                "tenant.context_missing",
                "An active tenant is required to create a workspace user.");

        var entity = mapper.Map<User>(command);
        entity.PasswordHash = passwordHasher.HashPassword(entity, command.Password);

        dbContext.Users.Add(entity);
        dbContext.TenantMemberships.Add(
            new TenantMembership(tenantId, entity.Id));

        await dbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
