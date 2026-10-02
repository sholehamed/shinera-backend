using ShineraApp.Application.Features.Plans;
using Web.Sharedkernel.Util;

namespace ShineraApp.Endpoints;

public sealed class PublicPlans : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGet("/api/public/plans", GetPlans)
            .AllowAnonymous()
            .WithTags("Public plans")
            .Produces<ApiResponse<IReadOnlyList<PublicPlanDto>>>();
    }

    public async Task<IResult> GetPlans(IDispatcher dispatcher, CancellationToken cancellationToken)
        => (await dispatcher.Query(new GetPublicPlansQuery(), cancellationToken)).ToHttpResult();
}
