using Domain.SharedKernel.Common;
using Domain.SharedKernel.Entities;

namespace Modules.System.Identity.Domain.Entities;

public sealed class BranchMembership : AuditableEntity, IMustHaveTenant
{
    public BranchMembership() : base(24)
    {
    }

    public BranchMembership(
        Guid tenantId,
        Guid branchId,
        Guid userId,
        bool isActive = true) : base(24)
    {
        TenantId = tenantId;
        BranchId = branchId;
        UserId = userId;
        IsActive = isActive;
    }

    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public bool IsActive { get; set; } = true;

    public Tenant Tenant { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
    public User User { get; set; } = default!;
}
