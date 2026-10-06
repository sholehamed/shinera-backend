namespace Modules.System.Workforce.Domain.Entities;

public sealed class StaffService : AuditableEntity, IMustHaveTenant
{
    public StaffService() : base(30)
    {
    }

    public StaffService(
        Guid tenantId,
        Guid staffId,
        Guid serviceId) : base(30)
    {
        TenantId = tenantId;
        StaffId = staffId;
        ServiceId = serviceId;
    }

    public Guid TenantId { get; set; }
    public Guid StaffId { get; set; }
    public Guid ServiceId { get; set; }

    public Staff Staff { get; set; } = default!;
}
