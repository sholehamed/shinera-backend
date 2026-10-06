using Modules.System.Identity.Application.Features.UiResources.Commands;
using Modules.System.Identity.Application.Authorization;
using Web.SharedKernel.Authorization;
using Modules.System.Identity.Application.Features.UiResources.Queries;

namespace Modules.System.Identity.Web.Endpoints
{
    public class UiResources: EndpointGroupBase
    {
        public override void Map(WebApplication app)
        {
            app.MapGroup(this)
                .MapPost(UiResourcePagedList, "{resourceId}/pagedList", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.List))
                .MapGet(UiResourceGetById, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.List))
                .MapGet(UiResourceLookup, "lookup", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.List))
                .MapPost(UiResourceCreate, configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.Create))
                .MapPut(UiResourceUpdate, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.Update))
                .MapDelete(UiResourceDelete, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.Delete))
                .MapPatch(UiResourceChangeState, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.Update));
        }
        public async Task UiResourceChangeState(IDispatcher sender, Guid id)
        {
            await sender.Send(new UiResourceChangeStateCommand(id));
        }

        public async Task UiResourceDelete(IDispatcher sender, Guid id)
        {
            await sender.Send(new UiResourceDeleteCommand(id));
        }
        public async Task<Results<Ok<List<LookupDto>>, BadRequest>> UiResourceLookup(IDispatcher sender, string? text, CancellationToken cancellationToken = default)
        {
            var res = await sender.Query(new UiResourceLookupQuery(text), cancellationToken);
            return TypedResults.Ok(res);
        }
        public async Task<Results<Ok<PagedList<UiResourcePagedListDto>>, BadRequest>> UiResourcePagedList(IDispatcher sender,Guid resourceId, UiResourcePagedListQuery parameter, CancellationToken cancellationToken = default)
        {
            parameter.ResourceId = resourceId;

            var res = await sender.Query(parameter, cancellationToken);
            return TypedResults.Ok(res);
        }

        public async Task<Results<Ok<UiResourceGetByIdDto>, NotFound>> UiResourceGetById([FromServices] IDispatcher sender, Guid id, CancellationToken cancellationToken = default)
        {
            var res = await sender.Query(new UiResourceGetByIdQuery(id), cancellationToken);
            return TypedResults.Ok(res);
        }
        public async Task<Results<Created<Guid>, BadRequest>> UiResourceCreate(IDispatcher sender, UiResourceCreateCommand parameter, CancellationToken cancellationToken = default)
        {
            var res = await sender.Send(parameter, cancellationToken);
            return TypedResults.Created(res.ToString(), res);
        }
        public async Task<NoContent> UiResourceUpdate([FromServices] IDispatcher sender, [FromRoute] Guid id, UiResourceUpdateCommand parameter, CancellationToken cancellationToken = default)
        {
            parameter.Id = id;
            await sender.Send(parameter, cancellationToken);
            return TypedResults.NoContent();
        }
    }
}
