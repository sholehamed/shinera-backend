namespace Modules.System.Subscription.Application.Entitlements;

public interface IEntitlementService
{
    Task<EntitlementDecision> HasFeatureAsync(
        string featureKey,
        CancellationToken cancellationToken = default);

    Task<LimitDecision> CheckLimitAsync(
        string limitKey,
        int currentUsage,
        int requestedIncrease = 1,
        CancellationToken cancellationToken = default);

    Task<EffectiveEntitlementsDto> GetEffectiveEntitlementsAsync(
        CancellationToken cancellationToken = default);
}
