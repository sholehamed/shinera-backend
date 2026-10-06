using Modules.System.Subscription.Application.Abstractions;
using Modules.System.Subscription.Domain.Entities;

namespace Modules.System.Subscription.Application.Entitlements;

public sealed class EntitlementService(
    IEntitlementDbContext dbContext,
    ICurrentTenant currentTenant,
    global::System.TimeProvider timeProvider)
    : IEntitlementService
{
    private Task<EffectiveEntitlementsDto>? _cachedResolution;

    public async Task<EntitlementDecision> HasFeatureAsync(
        string featureKey,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetEffectiveEntitlementsAsync(
            cancellationToken);

        var stateError = GetStateError(snapshot.Subscription.State);
        if (stateError is not null)
            return EntitlementDecision.Deny(stateError);

        var key = NormalizeKey(featureKey);

        return snapshot.Features.Contains(
            key,
            StringComparer.Ordinal)
                ? EntitlementDecision.Allow()
                : EntitlementDecision.Deny(
                    "subscription.feature_unavailable");
    }

    public async Task<LimitDecision> CheckLimitAsync(
        string limitKey,
        int currentUsage,
        int requestedIncrease = 1,
        CancellationToken cancellationToken = default)
    {
        if (currentUsage < 0)
            throw new ArgumentOutOfRangeException(nameof(currentUsage));

        if (requestedIncrease < 1)
            throw new ArgumentOutOfRangeException(nameof(requestedIncrease));

        var snapshot = await GetEffectiveEntitlementsAsync(
            cancellationToken);

        var stateError = GetStateError(snapshot.Subscription.State);
        if (stateError is not null)
        {
            return LimitDecision.Deny(
                null,
                currentUsage,
                requestedIncrease,
                stateError);
        }

        var key = NormalizeKey(limitKey);

        if (!snapshot.Limits.TryGetValue(key, out var limit))
        {
            return LimitDecision.Deny(
                null,
                currentUsage,
                requestedIncrease,
                "subscription.feature_unavailable");
        }

        if (currentUsage > limit)
        {
            return LimitDecision.Deny(
                limit,
                currentUsage,
                requestedIncrease,
                "subscription.over_limit");
        }

        if ((long)currentUsage + requestedIncrease > limit)
        {
            return LimitDecision.Deny(
                limit,
                currentUsage,
                requestedIncrease,
                "subscription.limit_reached");
        }

        return LimitDecision.Allow(
            limit,
            currentUsage,
            requestedIncrease);
    }

    public Task<EffectiveEntitlementsDto> GetEffectiveEntitlementsAsync(
        CancellationToken cancellationToken = default)
    {
        _cachedResolution ??= ResolveAsync(cancellationToken);
        return _cachedResolution;
    }

    private async Task<EffectiveEntitlementsDto> ResolveAsync(
        CancellationToken cancellationToken)
    {
        var tenantId = currentTenant.TenantId;
        if (!tenantId.HasValue)
        {
            return Empty(
                EffectiveSubscriptionState.Missing);
        }

        var now = timeProvider.GetUtcNow();

        var subscriptions = await dbContext.Subscriptions
            .AsNoTracking()
            .Where(subscription =>
                subscription.TenantId == tenantId.Value)
            .Include(subscription => subscription.Plan)
                .ThenInclude(plan => plan.Features)
                    .ThenInclude(planFeature => planFeature.Feature)
            .ToListAsync(cancellationToken);

        subscriptions = subscriptions
            .OrderByDescending(subscription =>
                subscription.StartedAtUtc)
            .ToList();

        if (subscriptions.Count == 0)
        {
            return Empty(
                EffectiveSubscriptionState.Missing);
        }

        var entitled = subscriptions
            .Where(subscription =>
                subscription.IsEntitled(now))
            .ToList();

        if (entitled.Count == 0)
        {
            var latest = subscriptions[0];

            return new EffectiveEntitlementsDto(
                new SubscriptionSummaryDto(
                    latest.Id,
                    latest.Plan.Key,
                    EffectiveSubscriptionState.Inactive,
                    latest.Status.ToString()),
                [],
                new Dictionary<string, int>(
                    StringComparer.Ordinal));
        }

        if (entitled.Count > 1)
        {
            return new EffectiveEntitlementsDto(
                new SubscriptionSummaryDto(
                    null,
                    null,
                    EffectiveSubscriptionState.Ambiguous,
                    null),
                [],
                new Dictionary<string, int>(
                    StringComparer.Ordinal));
        }

        var effective = entitled[0];

        var features = effective.Plan.Features
            .Where(planFeature =>
                planFeature.Feature.IsActive &&
                planFeature.Feature.Kind == FeatureKind.Boolean &&
                planFeature.IsEnabled)
            .Select(planFeature =>
                NormalizeKey(planFeature.Feature.Key))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        var limits = effective.Plan.Features
            .Where(planFeature =>
                planFeature.Feature.IsActive &&
                planFeature.Feature.Kind == FeatureKind.Limit &&
                planFeature.LimitValue.HasValue)
            .GroupBy(
                planFeature =>
                    NormalizeKey(planFeature.Feature.Key),
                StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Single().LimitValue!.Value,
                StringComparer.Ordinal);

        return new EffectiveEntitlementsDto(
            new SubscriptionSummaryDto(
                effective.Id,
                effective.Plan.Key,
                EffectiveSubscriptionState.Active,
                effective.Status.ToString()),
            features,
            limits);
    }

    private static EffectiveEntitlementsDto Empty(
        EffectiveSubscriptionState state) =>
        new(
            new SubscriptionSummaryDto(
                null,
                null,
                state,
                null),
            [],
            new Dictionary<string, int>(
                StringComparer.Ordinal));

    private static string? GetStateError(
        EffectiveSubscriptionState state) =>
        state switch
        {
            EffectiveSubscriptionState.Active => null,
            EffectiveSubscriptionState.Missing =>
                "subscription.required",
            _ =>
                "subscription.inactive"
        };

    private static string NormalizeKey(string key) =>
        key.Trim().ToLowerInvariant();
}
