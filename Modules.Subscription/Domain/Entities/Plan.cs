namespace Modules.System.Subscription.Domain.Entities;

public sealed class Plan : AuditableEntity
{
    public Plan()
    {
    }

    public Plan(
        string key,
        string name,
        string? description = null,
        int displayOrder = 0,
        bool isActive = true)
    {
        Key = NormalizeKey(key);
        Name = name.Trim();
        Description = description?.Trim();
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    public string Key { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ICollection<PlanFeature> Features { get; set; } = [];
    public ICollection<Subscription> Subscriptions { get; set; } = [];

    public static string NormalizeKey(string key) =>
        key.Trim().ToLowerInvariant();
}
