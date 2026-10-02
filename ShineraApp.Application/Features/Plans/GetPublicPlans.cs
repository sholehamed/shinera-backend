using Application.SharedKernel.Abstractions.Messaging;
using Application.Sharedkernel.Models;
using Microsoft.EntityFrameworkCore;
using ShineraApp.Application.Interfaces;
using ShineraApp.Domain;

namespace ShineraApp.Application.Features.Plans;

public sealed record PublicPlanPriceDto(string BillingCycle, decimal Amount, string Currency);
public sealed record PublicPlanFeatureDto(string Code, string Name, long? LimitValue);
public sealed record PublicPlanDto(
    string Key, string Title, string? Description, string Audience, int TrialDays,
    IReadOnlyList<PublicPlanPriceDto> Prices, IReadOnlyList<PublicPlanFeatureDto> Features);

public sealed record GetPublicPlansQuery : IQuery<Result<IReadOnlyList<PublicPlanDto>>>;

public sealed class GetPublicPlansHandler(IPlanCatalogDbContext db)
    : IQueryHandler<GetPublicPlansQuery, Result<IReadOnlyList<PublicPlanDto>>>
{
    public async Task<Result<IReadOnlyList<PublicPlanDto>>> Handle(
        GetPublicPlansQuery request, CancellationToken cancellationToken)
    {
        // Explicit soft-delete conditions also protect callers implementing the abstraction
        // without this context's global filters. Never publish private/inactive offerings.
        var plans = await db.Plans.AsNoTracking()
            .Where(p => p.IsActive && p.IsPublic && !p.IsDeleted)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Code)
            .Select(p => new PublicPlanDto(
                p.Code, p.Name, p.Description,
                p.Audience == PlanAudience.Solo ? "solo" : "salon", p.TrialDays,
                p.Prices.Where(x => x.IsActive && !x.IsDeleted)
                    .OrderBy(x => x.BillingPeriod).ThenBy(x => x.Currency)
                    .Select(x => new PublicPlanPriceDto(
                        x.BillingPeriod == BillingPeriod.Monthly ? "monthly" : "yearly",
                        x.Amount, x.Currency)).ToList(),
                p.Features.Where(x => x.IsEnabled && !x.IsDeleted &&
                        x.Feature.IsActive && x.Feature.IsVisible && !x.Feature.IsDeleted)
                    .OrderBy(x => x.Feature.DisplayOrder).ThenBy(x => x.Feature.Code)
                    .Select(x => new PublicPlanFeatureDto(x.Feature.Code, x.Feature.Name, x.LimitValue))
                    .ToList()))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<PublicPlanDto>>.Success(plans);
    }
}
