namespace Modules.System.Services.Domain.Entities;

public sealed class ServiceCategory : AuditableEntity, IMustHaveTenant
{
    public ServiceCategory() : base(26)
    {
    }

    public ServiceCategory(
        Guid tenantId,
        string name,
        string? description = null,
        int sortOrder = 0,
        bool isActive = true) : base(26)
    {
        TenantId = tenantId;
        Name = name.Trim();
        Description = Normalize(description);
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Service> Services { get; set; } = [];

    public void Update(
        string name,
        string? description,
        int sortOrder,
        bool isActive)
    {
        Name = name.Trim();
        Description = Normalize(description);
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
