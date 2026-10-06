namespace Modules.System.Workforce.Domain.Entities;

public sealed class StaffBranch : AuditableEntity, IMustHaveTenant
{
    public StaffBranch() : base(29)
    {
    }

    public StaffBranch(
        Guid tenantId,
        Guid staffId,
        Guid branchId) : base(29)
    {
        TenantId = tenantId;
        StaffId = staffId;
        BranchId = branchId;
    }

    public Guid TenantId { get; set; }
    public Guid StaffId { get; set; }
    public Guid BranchId { get; set; }

    public Staff Staff { get; set; } = default!;
}
