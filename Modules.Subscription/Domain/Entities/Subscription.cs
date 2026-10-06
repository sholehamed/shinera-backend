namespace Modules.System.Subscription.Domain.Entities;

public enum SubscriptionStatus
{
    Pending = 1,
    Active = 2,
    Suspended = 3,
    Cancelled = 4,
    Expired = 5
}

public sealed class Subscription : AuditableEntity, IMustHaveTenant
{
    public Subscription()
    {
    }

    public Subscription(
        Guid tenantId,
        Guid planId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset? endsAtUtc = null,
        SubscriptionStatus status = SubscriptionStatus.Pending)
    {
        TenantId = tenantId;
        PlanId = planId;
        StartedAtUtc = startedAtUtc.ToUniversalTime();
        EndsAtUtc = endsAtUtc?.ToUniversalTime();
        Status = status;
    }

    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? EndsAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? ExternalReference { get; set; }

    public Plan Plan { get; set; } = default!;

    public bool IsEntitled(DateTimeOffset nowUtc)
    {
        var now = nowUtc.ToUniversalTime();

        return Status == SubscriptionStatus.Active &&
               StartedAtUtc <= now &&
               (!EndsAtUtc.HasValue || now < EndsAtUtc.Value);
    }
}
