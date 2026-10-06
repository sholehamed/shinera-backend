using Application.SharedKernel.Models;
using System.Data.Common;

namespace Application.SharedKernel.Registration;

public sealed record RegistrationSubscriptionReceipt(
    Guid SubscriptionId,
    Guid PlanId,
    string PlanKey);

public interface IRegistrationSubscriptionProvisioner
{
    Task<Result<RegistrationSubscriptionReceipt>> ProvisionAsync(
        Guid tenantId,
        string planKey,
        DateTimeOffset startedAtUtc,
        DbTransaction transaction,
        CancellationToken cancellationToken = default);
}
