using Domain.SharedKernel.Entities;
using Domain.SharedKernel.Common;
namespace ShineraApp.Domain.Entities;

// Receipt also acts as the idempotency boundary. No credentials or raw requests are stored.
public sealed class WorkspaceRegistration : FullAuditableEntity, IMustHaveTenant
{
    private WorkspaceRegistration() { }
    public WorkspaceRegistration(Guid requestId, Guid tenantId, Guid ownerId, Guid branchId,
        string fingerprint, Guid planId, Guid priceId, DateTime startedAt, DateTime expiresAt)
    {
        RequestId = requestId; TenantId = tenantId; OwnerId = ownerId; BranchId = branchId;
        Fingerprint = fingerprint; PlanId = planId; PriceId = priceId;
        StartedAt = startedAt; ExpiresAt = expiresAt;
    }
    public Guid RequestId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid PlanId { get; private set; }
    public Guid PriceId { get; private set; }
    public string Fingerprint { get; private set; } = "";
    public DateTime StartedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
}
