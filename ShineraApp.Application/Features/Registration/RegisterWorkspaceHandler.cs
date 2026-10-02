using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.SharedKernel.Abstractions.Messaging;
using Application.Sharedkernel.Models;
using Domain.Sharedkernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShineraApp.Domain;
using ShineraApp.Domain.Entities;

namespace ShineraApp.Application.Features.Registration;

public sealed class RegisterWorkspaceHandler(IRegistrationDbContext db, IOwnerPasswordHasher passwords,
    IOptions<RegistrationOptions> options, TimeProvider clock)
    : ICommandHandler<RegisterWorkspaceCommand, Result<RegistrationReceipt>>
{
    public async Task<Result<RegistrationReceipt>> Handle(RegisterWorkspaceCommand command, CancellationToken ct)
    {
        if (!options.Value.Enabled)
            return Fail("Registration.Unavailable", "ثبت‌نام در حال حاضر فعال نیست.");
        var validation = await new RegisterWorkspaceValidator().ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<RegistrationReceipt>.Failure(Error.Validation("Registration.Invalid", "اطلاعات ثبت‌نام را بررسی کنید؛ رمز عبور باید حداقل ۱۲ کاراکتر باشد."));
        var request = command with {
            FirstName = command.FirstName.Trim(), LastName = command.LastName.Trim(),
            Email = command.Email.Trim().ToLowerInvariant(), BusinessName = command.BusinessName.Trim(),
            City = command.City.Trim(), Address = command.Address.Trim()
        };
        // Never include the password in a fast fingerprint or persisted request body.
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(request with { Password = "" }))));
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var existing = await db.Registrations.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == request.RequestId, ct);
        if (existing is not null)
        {
            var owner = await db.Owners.AsNoTracking().SingleOrDefaultAsync(x => x.Id == existing.OwnerId, ct);
            if (existing.Fingerprint != fingerprint || owner is null || !passwords.Verify(owner.PasswordHash, request.Password))
                return Fail("Registration.RequestConflict", "شناسه درخواست قبلاً برای اطلاعات دیگری استفاده شده است.");
            var tenant = await db.Tenants.AsNoTracking().SingleAsync(x => x.Id == existing.TenantId, ct);
            return Result<RegistrationReceipt>.Success(new(existing.TenantId, existing.BranchId, tenant.Slug));
        }
        var period = request.BillingCycle == "monthly" ? BillingPeriod.Monthly : BillingPeriod.Yearly;
        var plan = await db.Plans.AsNoTracking().Where(x => x.Code == request.PlanKey && x.IsActive && x.IsPublic && !x.IsDeleted)
            .Select(x => new { x.Id, x.Audience, Price = x.Prices.Where(p => p.IsActive && !p.IsDeleted && p.Currency == "IRR" && p.BillingPeriod == period)
                .Select(p => new { p.Id, p.Amount }).SingleOrDefault() }).SingleOrDefaultAsync(ct);
        if (plan?.Price is null || plan.Audience is not (PlanAudience.Solo or PlanAudience.Salon)) return Fail("Registration.PlanUnavailable", "پلن یا قیمت انتخابی دیگر در دسترس نیست.");
        if (plan.Price.Amount != 0) return Fail("Registration.PaymentRequired", "این پلن نیاز به پرداخت دارد؛ پرداخت آنلاین هنوز فعال نیست.");
        // Reserve deleted identities/slugs too; this global existence check exposes no tenant data.
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Slug == request.Slug, ct) ||
            await db.Owners.IgnoreQueryFilters().AnyAsync(x => x.Email == request.Email || x.Mobile == request.Mobile, ct))
            return Fail("Registration.Conflict", "ثبت‌نام با این اطلاعات ممکن نیست. اطلاعات حساب و نشانی فضای کاری را بررسی کنید.");
        var now = clock.GetUtcNow().UtcDateTime;
        var ownerAccount = new OwnerAccount(request.FirstName, request.LastName, request.Email, request.Mobile, passwords.Hash(request.Password));
        var workspace = new Tenant(request.BusinessName, request.Slug,
            plan.Audience == PlanAudience.Solo ? TenantType.Solo : TenantType.Salon);
        var branch = new Branch(workspace.Id, "شعبه اصلی", true);
        var profile = new BusinessProfile(workspace.Id, request.ActivityType, request.Phone, request.City,
            request.Address, request.PostalCode, request.Instagram);
        var membership = new TenantMembership(workspace.Id, ownerAccount.Id);
        var branchMembership = new BranchMembership(workspace.Id, ownerAccount.Id, branch.Id);
        var expiresAt = period == BillingPeriod.Monthly ? now.AddMonths(1) : now.AddYears(1);
        var subscription = new TenantSubscription(workspace.Id, plan.Id, plan.Price.Id, now, expiresAt);
        var receipt = new WorkspaceRegistration(request.RequestId, workspace.Id, ownerAccount.Id, branch.Id,
            fingerprint, plan.Id, plan.Price.Id, now, period == BillingPeriod.Monthly ? now.AddMonths(1) : now.AddYears(1));
        // Public registration is the only bootstrap operation without a current principal.
        // Attribute its audit records to the newly created owner; never trust a client OwnerId.
        foreach (var entity in new global::Domain.SharedKernel.Entities.FullAuditableEntity[] { ownerAccount, workspace, branch, profile, membership, branchMembership, subscription, receipt })
        { entity.CreatedAt = now; entity.CreatedBy = ownerAccount.Id; }
        db.Owners.Add(ownerAccount); db.Tenants.Add(workspace); db.Branches.Add(branch);
        db.BusinessProfiles.Add(profile); db.TenantMemberships.Add(membership); db.BranchMemberships.Add(branchMembership);
        db.Subscriptions.Add(subscription); db.Registrations.Add(receipt);
        try { await db.SaveChangesAsync(ct); }
        catch (RegistrationConflictException)
        { return Fail("Registration.Conflict", "اطلاعات ثبت‌نام هم‌زمان تغییر کرده است. دوباره تلاش کنید."); }
        await transaction.CommitAsync(ct);
        return Result<RegistrationReceipt>.Success(new(workspace.Id, branch.Id, workspace.Slug));
    }
    private static Result<RegistrationReceipt> Fail(string code, string message)
        => Result<RegistrationReceipt>.Failure(Error.Conflict(code, message));
}
