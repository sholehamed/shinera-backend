using Modules.System.Subscription.Application.Entitlements;

namespace Modules.System.Subscription.Web.Endpoints;

public sealed class Entitlements : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(
                Current,
                "current",
                configure: endpoint =>
                    endpoint.RequireAuthorization());
    }

    public async Task<IResult> Current(
        IEntitlementService entitlementService,
        CancellationToken cancellationToken)
    {
        var entitlements =
            await entitlementService.GetEffectiveEntitlementsAsync(
                cancellationToken);

        return Results.Ok(entitlements);
    }
}
