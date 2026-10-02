using Domain.SharedKernel.Entities;
using Domain.SharedKernel.Common;
namespace ShineraApp.Domain.Entities;
public sealed class TenantSubscription : FullAuditableEntity, IMustHaveTenant
{
    private TenantSubscription() { }
    public TenantSubscription(Guid tenantId, Guid planId, Guid priceId, DateTime startsAt, DateTime expiresAt)
    {
        if (expiresAt <= startsAt) throw new ArgumentException("Subscription must have a positive duration.");
        TenantId = tenantId; PlanId = planId; PriceId = priceId; StartsAt = startsAt; ExpiresAt = expiresAt;
    }
    public Guid TenantId { get; private set; }
    public Guid PlanId { get; private set; }
    public Guid PriceId { get; private set; }
    public decimal Amount { get; private set; } // Only explicit zero-price registrations are supported.
    public string Currency { get; private set; } = "IRR";
    public DateTime StartsAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
}
