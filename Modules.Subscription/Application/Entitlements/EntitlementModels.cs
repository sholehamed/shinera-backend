namespace Modules.System.Subscription.Application.Entitlements;

public enum EffectiveSubscriptionState
{
    Missing = 1,
    Inactive = 2,
    Active = 3,
    Ambiguous = 4
}

public sealed record SubscriptionSummaryDto(
    Guid? SubscriptionId,
    string? PlanKey,
    EffectiveSubscriptionState State,
    string? Status);

public sealed record EffectiveEntitlementsDto(
    SubscriptionSummaryDto Subscription,
    IReadOnlyList<string> Features,
    IReadOnlyDictionary<string, int> Limits);

public sealed record EntitlementDecision(
    bool IsAllowed,
    string? ErrorCode)
{
    public static EntitlementDecision Allow() =>
        new(true, null);

    public static EntitlementDecision Deny(string errorCode) =>
        new(false, errorCode);
}

public sealed record LimitDecision(
    bool IsAllowed,
    int? Limit,
    int CurrentUsage,
    int RequestedIncrease,
    string? ErrorCode)
{
    public static LimitDecision Allow(
        int limit,
        int currentUsage,
        int requestedIncrease) =>
        new(true, limit, currentUsage, requestedIncrease, null);

    public static LimitDecision Deny(
        int? limit,
        int currentUsage,
        int requestedIncrease,
        string errorCode) =>
        new(false, limit, currentUsage, requestedIncrease, errorCode);
}
