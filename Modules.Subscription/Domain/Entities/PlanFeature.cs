namespace Modules.System.Subscription.Domain.Entities;

public sealed class PlanFeature : AuditableEntity
{
    public PlanFeature()
    {
    }

    public PlanFeature(
        Guid planId,
        Guid featureId,
        bool isEnabled = false,
        int? limitValue = null)
    {
        PlanId = planId;
        FeatureId = featureId;
        IsEnabled = isEnabled;
        LimitValue = limitValue;
    }

    public Guid PlanId { get; set; }
    public Guid FeatureId { get; set; }
    public bool IsEnabled { get; set; }
    public int? LimitValue { get; set; }

    public Plan Plan { get; set; } = default!;
    public Feature Feature { get; set; } = default!;
}
