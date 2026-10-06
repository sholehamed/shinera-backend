namespace Modules.System.Subscription.Domain.Entities;

public enum FeatureKind
{
    Boolean = 1,
    Limit = 2
}

public sealed class Feature : AuditableEntity
{
    public Feature()
    {
    }

    public Feature(
        string key,
        string name,
        FeatureKind kind,
        string? description = null,
        bool isActive = true)
    {
        Key = NormalizeKey(key);
        Name = name.Trim();
        Kind = kind;
        Description = description?.Trim();
        IsActive = isActive;
    }

    public string Key { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public FeatureKind Kind { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PlanFeature> Plans { get; set; } = [];

    public static string NormalizeKey(string key) =>
        key.Trim().ToLowerInvariant();
}
