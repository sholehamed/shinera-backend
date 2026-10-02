using Domain.SharedKernel.Entities;
using Domain.SharedKernel.Common;
namespace ShineraApp.Domain.Entities;
public sealed class BranchMembership : FullAuditableEntity, IMustHaveTenant
{
    private BranchMembership() { }
    public BranchMembership(Guid tenantId, Guid ownerId, Guid branchId)
    { TenantId = tenantId; OwnerId = ownerId; BranchId = branchId; }
    public Guid TenantId { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid BranchId { get; private set; }
}
