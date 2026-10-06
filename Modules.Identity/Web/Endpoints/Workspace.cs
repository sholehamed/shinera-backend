using Modules.System.Identity.Application.Features.Workspace.Queries;

namespace Modules.System.Identity.Web.Endpoints;

public sealed class Workspace : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(Current, "current");
    }

    public async Task<Ok<CurrentWorkspaceDto>> Current(
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Query(
            new CurrentWorkspaceQuery(),
            cancellationToken);

        return TypedResults.Ok(result);
    }
}
