using Domain.SharedKernel.Common;
using Domain.SharedKernel.Entities;

namespace Modules.System.Identity.Domain.Entities;

public sealed class Branch : AuditableEntity, IMustHaveTenant
{
    public Branch() : base(23)
    {
    }

    public Branch(
        Guid tenantId,
        string name,
        string? phone = null,
        string? address = null,
        bool isMain = false,
        bool isActive = true) : base(23)
    {
        TenantId = tenantId;
        Name = name;
        Phone = phone;
        Address = address;
        IsMain = isMain;
        IsActive = isActive;
    }

    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsMain { get; set; }
    public bool IsActive { get; set; } = true;

    public Tenant Tenant { get; set; } = default!;
    public ICollection<BranchMembership> Memberships { get; set; } = [];
}
