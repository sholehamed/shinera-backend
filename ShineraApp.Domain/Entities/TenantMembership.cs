using Domain.SharedKernel.Entities;
using Domain.SharedKernel.Common;
namespace ShineraApp.Domain.Entities;
public sealed class TenantMembership : FullAuditableEntity, IMustHaveTenant
{
    private TenantMembership() { }
    public TenantMembership(Guid tenantId, Guid ownerId)
    { TenantId = tenantId; OwnerId = ownerId;  }
    public Guid TenantId { get; private set; }
    public Guid OwnerId { get; private set; }
    public bool IsOwner { get; private set; } = true;
}
