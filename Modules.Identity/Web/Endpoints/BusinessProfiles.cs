using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Application.Features.BusinessProfiles;
using Web.SharedKernel.Authorization;

namespace Modules.System.Identity.Web.Endpoints;

public sealed class BusinessProfiles : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(
                Current,
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.BusinessProfile.Resource,
                        SystemPermissionCatalog.BusinessProfile.View))
            .MapPut(
                Update,
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.BusinessProfile.Resource,
                        SystemPermissionCatalog.BusinessProfile.Update));
    }

    public async Task<Ok<BusinessProfileDto>> Current(
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Query(
            new CurrentBusinessProfileQuery(),
            cancellationToken);

        return TypedResults.Ok(result);
    }

    public async Task<NoContent> Update(
        IDispatcher dispatcher,
        UpdateBusinessProfileCommand command,
        CancellationToken cancellationToken)
    {
        await dispatcher.Send(
            command,
            cancellationToken);

        return TypedResults.NoContent();
    }
}
