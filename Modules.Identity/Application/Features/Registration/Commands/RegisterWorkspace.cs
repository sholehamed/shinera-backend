using Application.SharedKernel.Registration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Application.Features.Registration.Commands;

public sealed record RegisterWorkspaceCommand(
    string PlanKey,
    RegistrationBusiness Business,
    RegistrationOwner Owner,
    RegistrationBranch Branch)
    : ICommand<Result<RegistrationResult>>;

public sealed record RegistrationBusiness(
    string Name,
    string BusinessType,
    BusinessMode Mode,
    string? Phone,
    string? Email,
    string? City,
    string? Address);

public sealed record RegistrationOwner(
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    string Password);

public sealed record RegistrationBranch(
    string Name,
    string? Phone,
    string? Address);

public sealed record RegistrationResult(
    Guid UserId,
    string UserName,
    string Email,
    Guid TenantId,
    string TenantSlug,
    Guid BranchId,
    Guid BusinessProfileId,
    Guid SubscriptionId,
    string PlanKey);

public sealed class RegisterWorkspaceCommandValidator
    : AbstractValidator<RegisterWorkspaceCommand>
{
    public RegisterWorkspaceCommandValidator()
    {
        RuleFor(x => x.PlanKey)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Business.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Business.BusinessType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Business.Mode)
            .IsInEnum();

        RuleFor(x => x.Business.Phone)
            .MaximumLength(32);

        RuleFor(x => x.Business.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Business.Email))
            .MaximumLength(256);

        RuleFor(x => x.Business.City)
            .MaximumLength(150);

        RuleFor(x => x.Business.Address)
            .MaximumLength(500);

        RuleFor(x => x.Owner.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Owner.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Owner.Phone)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Owner.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Owner.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(200);

        RuleFor(x => x.Branch.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Branch.Phone)
            .MaximumLength(32);

        RuleFor(x => x.Branch.Address)
            .MaximumLength(500);
    }
}

public sealed class RegisterWorkspaceCommandHandler(
    IIdentityDbContext db,
    ITenantContext tenantContext,
    IPasswordHasher<User> passwordHasher,
    IRegistrationSubscriptionProvisioner subscriptionProvisioner,
    TimeProvider timeProvider)
    : ICommandHandler<RegisterWorkspaceCommand, Result<RegistrationResult>>
{
    public async Task<Result<RegistrationResult>> Handle(
        RegisterWorkspaceCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedEmail =
            command.Owner.Email.Trim().ToUpperInvariant();

        using (tenantContext.DisableFilter())
        {
            var emailExists = await db.Users
                .AsNoTracking()
                .AnyAsync(
                    x => x.NormalizedEmail == normalizedEmail,
                    cancellationToken);

            if (emailExists)
            {
                return Result<RegistrationResult>.Failure(
                    Error.Conflict(
                        "registration.owner_email_exists",
                        "An account already exists with this email address."));
            }

            var permissionIds = await db.Permissions
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.Resource != null &&
                    x.Resource.IsActive &&
                    SystemPermissionCatalog.WorkspaceOwnerPermissionKeys
                        .Contains(x.Code))
                .Select(x => x.Id)
                .ToArrayAsync(cancellationToken);

            if (permissionIds.Length !=
                SystemPermissionCatalog.WorkspaceOwnerPermissionKeys.Length)
            {
                return Result<RegistrationResult>.Failure(
                    new Error(
                        "registration.permission_catalog_incomplete",
                        "The workspace permission catalog is not ready."));
            }

            var tenantId = Guid.CreateVersion7();
            var userId = Guid.CreateVersion7();
            var slug = CreateTenantSlug(tenantId);

            var tenant = new Tenant(tenantId)
            {
                Name = command.Business.Name.Trim(),
                Slug = slug,
                IsActive = true
            };

            var user = new User(
                userId,
                $"owner-{userId:N}",
                command.Owner.Email.Trim(),
                command.Owner.FirstName.Trim(),
                command.Owner.LastName.Trim())
            {
                Phone = command.Owner.Phone.Trim(),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                EmailConfirmed = false
            };

            user.PasswordHash =
                passwordHasher.HashPassword(
                    user,
                    command.Owner.Password);

            var mainBranch = new Branch(
                tenantId,
                command.Branch.Name.Trim(),
                Normalize(command.Branch.Phone),
                Normalize(command.Branch.Address),
                isMain: true);

            var businessProfile = new BusinessProfile(
                tenantId,
                command.Business.Name,
                command.Business.BusinessType,
                command.Business.Mode,
                command.Business.Phone,
                command.Business.Email,
                command.Business.City,
                command.Business.Address);

            var ownerRole = new Role(
                tenantId,
                "Owner",
                "Workspace owner");

            db.Tenants.Add(tenant);
            db.Users.Add(user);
            db.BusinessProfiles.Add(businessProfile);
            db.Branches.Add(mainBranch);

            db.TenantMemberships.Add(
                new TenantMembership(
                    tenantId,
                    userId));

            db.BranchMemberships.Add(
                new BranchMembership(
                    tenantId,
                    mainBranch.Id,
                    userId));

            db.Roles.Add(ownerRole);
            db.UserRoles.Add(
                new UserRole(
                    tenantId,
                    userId,
                    ownerRole.Id));

            db.PermissionAssignments.AddRange(
                permissionIds.Select(
                    permissionId =>
                        new PermissionAssignment(
                            tenantId,
                            permissionId,
                            PermissionSubjectType.Role,
                            ownerRole.Id,
                            PermissionScopeType.Tenant)));

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                await db.SaveChangesAsync(cancellationToken);

                var subscriptionResult =
                    await subscriptionProvisioner.ProvisionAsync(
                        tenantId,
                        command.PlanKey,
                        timeProvider.GetUtcNow(),
                        transaction.GetDbTransaction(),
                        cancellationToken);

                if (subscriptionResult.IsFailure)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return Result<RegistrationResult>.Failure(
                        subscriptionResult.Error);
                }

                await transaction.CommitAsync(cancellationToken);

                var subscription =
                    subscriptionResult.Value;

                return Result<RegistrationResult>.Success(
                    new RegistrationResult(
                        user.Id,
                        user.UserName,
                        user.Email,
                        tenant.Id,
                        tenant.Slug,
                        mainBranch.Id,
                        businessProfile.Id,
                        subscription.SubscriptionId,
                        subscription.PlanKey));
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    private static string CreateTenantSlug(Guid tenantId) =>
        $"business-{tenantId:N}";

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
