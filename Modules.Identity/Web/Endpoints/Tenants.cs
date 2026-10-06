using Modules.System.Identity.Application.Features.Tenants.Commands;
using Modules.System.Identity.Application.Authorization;
using Web.SharedKernel.Authorization;
using Modules.System.Identity.Application.Features.Tenants.Queries;


namespace Modules.System.Identity.Web.Endpoints;

public class Tenants : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapPost(TenantPagedList, "pagedList", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Tenants.Resource, SystemPermissionCatalog.Tenants.List))
            .MapGet(TenantResolveBySlug, "resolve", configure: endpoint => endpoint.AllowAnonymous())
            .MapGet(TenantGetById, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Tenants.Resource, SystemPermissionCatalog.Tenants.List))
            .MapPost(TenantCreate, configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Tenants.Resource, SystemPermissionCatalog.Tenants.Create))
            .MapPut(TenantUpdate, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Tenants.Resource, SystemPermissionCatalog.Tenants.Update))
            .MapDelete(TenantDelete, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Tenants.Resource, SystemPermissionCatalog.Tenants.Delete))
            .MapPatch(TenantChangeState, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Tenants.Resource, SystemPermissionCatalog.Tenants.Update));
    }
    public async Task<Results<Ok<TenantResloveBySlugDto>, NotFound>> TenantResolveBySlug(IDispatcher sender,HttpContext httpContext)
    {
        var slug = httpContext.Request.Headers["X-Tenant-Domain"].ToString();

        var res = await sender.Query(new TenantResolveBySlugQuery(slug));
        return TypedResults.Ok(res);
    }
    public async Task TenantChangeState(IDispatcher sender, Guid id)
    {
        await sender.Send(new TenantChangeStateCommand(id));
    }

    public async Task TenantDelete(IDispatcher sender, Guid id)
    {
        await sender.Send(new TenantDeleteCommand(id));
    }

    public async Task<Results<Ok<PagedList<TenantPagedListDto>>, BadRequest>> TenantPagedList(IDispatcher sender, TenantPagedListQuery parameter, CancellationToken cancellationToken = default)
    {
        var res = await sender.Query(parameter, cancellationToken);
        return TypedResults.Ok(res);
    }

    public async Task<Results<Ok<TenantGetByIdDto>, NotFound>> TenantGetById([FromServices] IDispatcher sender, Guid id, CancellationToken cancellationToken = default)
    {
        var res = await sender.Query(new TenantGetByIdQuery(id), cancellationToken);
        return TypedResults.Ok(res);
    }
    public async Task<Results<Created<Guid>, BadRequest>> TenantCreate(IDispatcher sender, TenantCreateCommand parameter, CancellationToken cancellationToken = default)
    {
        var res = await sender.Send(parameter, cancellationToken);
        return TypedResults.Created(res.ToString(), res);
    }
    public async Task<NoContent> TenantUpdate([FromServices] IDispatcher sender, [FromRoute] Guid id, TenantUpdateCommand parameter, CancellationToken cancellationToken = default)
    {
        parameter.Id = id;
        await sender.Send(parameter, cancellationToken);
        return TypedResults.NoContent();
    }


}