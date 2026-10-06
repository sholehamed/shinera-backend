using Application.SharedKernel.Exceptions;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Application.Features.Branches.Commands;

public sealed record BranchCreateCommand(
    string Name,
    string? Phone,
    string? Address) : ICommand<Guid>;

public sealed class BranchCreateCommandValidator
    : AbstractValidator<BranchCreateCommand>
{
    public BranchCreateCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .MaximumLength(32);

        RuleFor(x => x.Address)
            .MaximumLength(500);
    }
}

public sealed class BranchCreateCommandHandler(
    IIdentityDbContext db,
    ITenantContext tenantContext,
    IPermissionAuthorizationService authorizationService)
    : ICommandHandler<BranchCreateCommand, Guid>
{
    public async Task<Guid> Handle(
        BranchCreateCommand command,
        CancellationToken cancellationToken)
    {
        await BranchPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            tenantContext,
            SystemPermissionCatalog.Branches.Create,
            cancellationToken);

        var tenantId = tenantContext.ActiveTenantId
            ?? throw new TenantAccessException(
                "tenant.context_missing",
                "A tenant context is required for this operation.");

        var userId = tenantContext.UserId
            ?? throw new TenantAccessException(
                "tenant.user_context_invalid",
                "The authenticated user context is invalid.");

        var branch = new Branch(
            tenantId,
            command.Name.Trim(),
            Normalize(command.Phone),
            Normalize(command.Address));

        db.Branches.Add(branch);
        db.BranchMemberships.Add(
            new BranchMembership(
                tenantId,
                branch.Id,
                userId));

        await db.SaveChangesAsync(cancellationToken);

        return branch.Id;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}

public sealed record BranchUpdateCommand(
    Guid Id,
    string Name,
    string? Phone,
    string? Address) : ICommand;

public sealed class BranchUpdateCommandValidator
    : AbstractValidator<BranchUpdateCommand>
{
    public BranchUpdateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .MaximumLength(32);

        RuleFor(x => x.Address)
            .MaximumLength(500);
    }
}

public sealed class BranchUpdateCommandHandler(
    IIdentityDbContext db,
    ITenantContext tenantContext,
    IPermissionAuthorizationService authorizationService)
    : ICommandHandler<BranchUpdateCommand>
{
    public async Task Handle(
        BranchUpdateCommand command,
        CancellationToken cancellationToken)
    {
        await BranchPermissionGuard.RequireBranchScopeAsync(
            authorizationService,
            tenantContext,
            SystemPermissionCatalog.Branches.Update,
            command.Id,
            cancellationToken);

        var branch = await db.Branches
            .SingleOrDefaultAsync(
                x => x.Id == command.Id,
                cancellationToken);

        Guard.Against.NotFound(command.Id, branch);

        branch.Name = command.Name.Trim();
        branch.Phone = Normalize(command.Phone);
        branch.Address = Normalize(command.Address);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}

public sealed record BranchDisableCommand(Guid Id) : ICommand<Result>;

public sealed class BranchDisableCommandHandler(
    IIdentityDbContext db,
    ITenantContext tenantContext,
    IPermissionAuthorizationService authorizationService)
    : ICommandHandler<BranchDisableCommand, Result>
{
    public async Task<Result> Handle(
        BranchDisableCommand command,
        CancellationToken cancellationToken)
    {
        await BranchPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            tenantContext,
            SystemPermissionCatalog.Branches.Disable,
            cancellationToken);

        var branch = await db.Branches
            .SingleOrDefaultAsync(
                x => x.Id == command.Id,
                cancellationToken);

        Guard.Against.NotFound(command.Id, branch);

        if (branch.IsMain)
        {
            return Result.Failure(
                Error.Conflict(
                    "branch.main_disable_forbidden",
                    "The main branch cannot be disabled. Select another main branch first."));
        }

        if (!branch.IsActive)
            return Result.Success();

        branch.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed record SetMainBranchCommand(Guid BranchId) : ICommand<Result>;

public sealed class SetMainBranchCommandHandler(
    IIdentityDbContext db,
    ITenantContext tenantContext,
    IPermissionAuthorizationService authorizationService)
    : ICommandHandler<SetMainBranchCommand, Result>
{
    public async Task<Result> Handle(
        SetMainBranchCommand command,
        CancellationToken cancellationToken)
    {
        await BranchPermissionGuard.RequireTenantScopeAsync(
            authorizationService,
            tenantContext,
            SystemPermissionCatalog.Branches.SetMain,
            cancellationToken);

        var target = await db.Branches
            .SingleOrDefaultAsync(
                x => x.Id == command.BranchId,
                cancellationToken);

        Guard.Against.NotFound(command.BranchId, target);

        if (!target.IsActive)
        {
            return Result.Failure(
                Error.Conflict(
                    "branch.inactive_main_forbidden",
                    "An inactive branch cannot become the main branch."));
        }

        if (target.IsMain)
            return Result.Success();

        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        var currentMain = await db.Branches
            .SingleOrDefaultAsync(
                x => x.IsMain,
                cancellationToken);

        if (currentMain is not null)
        {
            currentMain.IsMain = false;
            await db.SaveChangesAsync(cancellationToken);
        }

        target.IsMain = true;
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
