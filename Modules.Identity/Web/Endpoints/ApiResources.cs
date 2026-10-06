using Modules.System.Identity.Application.Features.ApiResources.Commands;
using Modules.System.Identity.Application.Authorization;
using Web.SharedKernel.Authorization;
using Modules.System.Identity.Application.Features.ApiResources.Queries;

namespace Modules.System.Identity.Web.Endpoints
{
    public class ApiResources: EndpointGroupBase
    {
        public override void Map(WebApplication app)
        {
            app.MapGroup(this)
                .MapPost(ApiResourcePagedList, "{resourceId}/pagedList", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.List))
                .MapPatch(ApiResourceSync, "{resourceId}/sync", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.Update))
                .MapGet(ApiResourceGetById, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.List))
                .MapGet(ApiResourceLookup, "lookup", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.List))
                .MapPost(ApiResourceCreate, displayName: "ایجاد api جدید", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.Create))
                .MapPut(ApiResourceUpdate, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.Update))
                .MapDelete(ApiResourceDelete, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.Delete))
                .MapPatch(ApiResourceChangeState, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Resources.Resource, SystemPermissionCatalog.Resources.Update));
        }
        public async Task ApiResourceSync(IDispatcher sender,Guid resourceId)
        {
            await sender.Send(new ApiResourceSyncCommand(resourceId));
        }
        public async Task ApiResourceChangeState(IDispatcher sender, Guid id)
        {
            await sender.Send(new ApiResourceChangeStateCommand(id));
        }

        public async Task ApiResourceDelete(IDispatcher sender, Guid id)
        {
            await sender.Send(new ApiResourceDeleteCommand(id));
        }
        public async Task<Results<Ok<List<LookupDto>>, BadRequest>> ApiResourceLookup(IDispatcher sender, string? text, CancellationToken cancellationToken = default)
        {
            var res = await sender.Query(new ApiResourceLookupQuery(text), cancellationToken);
            return TypedResults.Ok(res);
        }
        public async Task<Results<Ok<PagedList<ApiResourcePagedListDto>>, BadRequest>> ApiResourcePagedList(IDispatcher sender,Guid resourceId ,ApiResourcePagedListQuery parameter, CancellationToken cancellationToken = default)
        {
            parameter.ResourceId = resourceId;
            var res = await sender.Query(parameter, cancellationToken);
            return TypedResults.Ok(res);
        }

        public async Task<Results<Ok<ApiResourceGetByIdDto>, NotFound>> ApiResourceGetById([FromServices] IDispatcher sender, Guid id, CancellationToken cancellationToken = default)
        {
            var res = await sender.Query(new ApiResourceGetByIdQuery(id), cancellationToken);
            return TypedResults.Ok(res);
        }
        public async Task<Results<Created<Guid>, BadRequest>> ApiResourceCreate(IDispatcher sender, ApiResourceCreateCommand parameter, CancellationToken cancellationToken = default)
        {
            var res = await sender.Send(parameter, cancellationToken);
            return TypedResults.Created(res.ToString(), res);
        }
        public async Task<NoContent> ApiResourceUpdate([FromServices] IDispatcher sender, [FromRoute] Guid id, ApiResourceUpdateCommand parameter, CancellationToken cancellationToken = default)
        {
            parameter.Id = id;
            await sender.Send(parameter, cancellationToken);
            return TypedResults.NoContent();
        }
    }
}
