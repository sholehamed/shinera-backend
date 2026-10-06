using Application.SharedKernel.Models;
using Application.SharedKernel.Registration;
using Microsoft.EntityFrameworkCore.Storage;
using Modules.System.Subscription.Domain.Entities;
using Modules.System.Subscription.Infrastructure.Persistence.Contexts;
using System.Data.Common;
using SubscriptionEntity = Modules.System.Subscription.Domain.Entities.Subscription;

namespace Modules.System.Subscription.Application.Registration;

public sealed class RegistrationSubscriptionProvisioner(
    SubscriptionDbContext db)
    : IRegistrationSubscriptionProvisioner
{
    public async Task<Result<RegistrationSubscriptionReceipt>> ProvisionAsync(
        Guid tenantId,
        string planKey,
        DateTimeOffset startedAtUtc,
        DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var transactionConnection = transaction.Connection
            ?? throw new InvalidOperationException(
                "The registration transaction has no active database connection.");

        // Registration is the only cross-module workflow that needs a shared
        // physical connection. Rebind this scoped context just for the
        // workflow, then enlist it before the first command executes.
        db.Database.SetDbConnection(
            transactionConnection,
            contextOwnsConnection: false);

        await db.Database.UseTransactionAsync(
            transaction,
            cancellationToken);

        var normalizedPlanKey = Plan.NormalizeKey(planKey);

        var plan = await db.Plans
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Key == normalizedPlanKey && x.IsActive,
                cancellationToken);

        if (plan is null)
        {
            return Result<RegistrationSubscriptionReceipt>.Failure(
                Error.Validation(
                    "registration.plan_invalid",
                    "The selected plan does not exist or is not active."));
        }

        var existingSubscription = await db.Subscriptions
            .IgnoreQueryFilters(["tenant"])
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId,
                cancellationToken);

        if (existingSubscription)
        {
            return Result<RegistrationSubscriptionReceipt>.Failure(
                Error.Conflict(
                    "registration.subscription_exists",
                    "A subscription already exists for this tenant."));
        }

        var subscription = new SubscriptionEntity(
            tenantId,
            plan.Id,
            startedAtUtc,
            status: SubscriptionStatus.Pending);

        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync(cancellationToken);

        return Result<RegistrationSubscriptionReceipt>.Success(
            new RegistrationSubscriptionReceipt(
                subscription.Id,
                plan.Id,
                plan.Key));
    }
}
