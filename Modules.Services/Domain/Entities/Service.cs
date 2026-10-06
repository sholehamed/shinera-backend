namespace Modules.System.Services.Domain.Entities;

public sealed class Service : AuditableEntity, IMustHaveTenant
{
    public Service() : base(27)
    {
    }

    public Service(
        Guid tenantId,
        Guid categoryId,
        string name,
        int durationMinutes,
        decimal price,
        string? description = null,
        bool isActive = true) : base(27)
    {
        TenantId = tenantId;
        CategoryId = categoryId;
        Name = name.Trim();
        Description = Normalize(description);
        DurationMinutes = durationMinutes;
        Price = price;
        IsActive = isActive;
    }

    public Guid TenantId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    public ServiceCategory Category { get; set; } = default!;

    public void Update(
        Guid categoryId,
        string name,
        int durationMinutes,
        decimal price,
        string? description,
        bool isActive)
    {
        CategoryId = categoryId;
        Name = name.Trim();
        Description = Normalize(description);
        DurationMinutes = durationMinutes;
        Price = price;
        IsActive = isActive;
    }

    public void SetActive(bool isActive) =>
        IsActive = isActive;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
