using Modules.System.Identity.Application.Features.Resources.Commands;
using Modules.System.Identity.Application.Authorization;
using Web.SharedKernel.Authorization;
using Modules.System.Identity.Application.Features.Resources.Queries;

namespace Modules.System.Identity.Web.Endpoints
{
    public class Resources: EndpointGroupBase
    {
        public override void Map(WebApplication app)
        {
            app.MapGroup(this)
                .MapPost(ResourcePagedList, "pagedList", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.List))
                .MapGet(ResourceGetById, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.List))
                .MapGet(ResourceLookup, "lookup", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.List))
                .MapGet(ResourceResourcesLookup, "{resourceId}/resources", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.List))
                .MapPost(ResourceCreate, configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.Create))
                .MapPut(ResourceUpdate, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.Update))
                .MapDelete(ResourceDelete, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.Delete))
                .MapPatch(ResourceChangeState, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.Update));
        }
        public async Task ResourceChangeState(IDispatcher sender, Guid id)
        {
            await sender.Send(new ResourceChangeStateCommand(id));
        }

        public async Task ResourceDelete(IDispatcher sender, Guid id)
        {
            await sender.Send(new ResourceDeleteCommand(id));
        }
        public async Task<Results<Ok<List<LookupDto>>, BadRequest>> ResourceLookup(IDispatcher sender, string? text, CancellationToken cancellationToken = default)
        {
            var res = await sender.Query(new ResourceLookupQuery(text), cancellationToken);
            return TypedResults.Ok(res);
        }
        public async Task<Results<Ok<List<GroupedLookupDto>>, BadRequest>> ResourceResourcesLookup(IDispatcher sender,Guid resourceId, string? text, CancellationToken cancellationToken = default)
        {
            var res = await sender.Query(new ResourceGetApiUiResourcesLookupQuery(resourceId,text), cancellationToken);
            return TypedResults.Ok(res);
        }
        public async Task<Results<Ok<PagedList<ResourcePagedListDto>>, BadRequest>> ResourcePagedList(IDispatcher sender, ResourcePagedListQuery parameter, CancellationToken cancellationToken = default)
        {
            var res = await sender.Query(parameter, cancellationToken);
            return TypedResults.Ok(res);
        }

        public async Task<Results<Ok<ResourceGetByIdDto>, NotFound>> ResourceGetById([FromServices] IDispatcher sender, Guid id, CancellationToken cancellationToken = default)
        {
            var res = await sender.Query(new ResourceGetByIdQuery(id), cancellationToken);
            return TypedResults.Ok(res);
        }
        public async Task<Results<Created<Guid>, BadRequest>> ResourceCreate(IDispatcher sender, ResourceCreateCommand parameter, CancellationToken cancellationToken = default)
        {
            var res = await sender.Send(parameter, cancellationToken);
            return TypedResults.Created(res.ToString(), res);
        }
        public async Task<NoContent> ResourceUpdate([FromServices] IDispatcher sender, [FromRoute] Guid id, ResourceUpdateCommand parameter, CancellationToken cancellationToken = default)
        {
            parameter.Id = id;
            await sender.Send(parameter, cancellationToken);
            return TypedResults.NoContent();
        }
    }
}
