using Domain.SharedKernel.Common;
using Domain.SharedKernel.Entities;

namespace Modules.System.Identity.Domain.Entities;

public sealed class TenantMembership : AuditableEntity, IMustHaveTenant
{
    public TenantMembership()
    {
    }

    public TenantMembership(Guid tenantId, Guid userId, bool isActive = true)
    {
        TenantId = tenantId;
        UserId = userId;
        IsActive = isActive;
    }

    public TenantMembership(Guid id, Guid tenantId, Guid userId, bool isActive = true)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        IsActive = isActive;
    }

    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = default!;
    public Tenant Tenant { get; set; } = default!;
}
