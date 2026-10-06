namespace Modules.System.Crm.Domain.Entities;

public sealed class CustomerNote : AuditableEntity, IMustHaveTenant
{
    public CustomerNote() : base(34)
    {
    }

    public CustomerNote(
        Guid tenantId,
        Guid customerId,
        string content) : base(34)
    {
        TenantId = tenantId;
        CustomerId = customerId;
        Content = content.Trim();
    }

    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public string Content { get; set; } = default!;

    public Customer Customer { get; set; } = default!;
}
