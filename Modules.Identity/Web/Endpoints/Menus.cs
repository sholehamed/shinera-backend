using Microsoft.AspNetCore.Routing;
using Modules.System.Identity.Application.Authorization;
using Web.SharedKernel.Authorization;
using Modules.System.Identity.Application.Features.Menus.Commands;
using Modules.System.Identity.Application.Features.Menus.Queries;


namespace Modules.System.Identity.Web.Endpoints;

public class Menus : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapPost(ReorderMenus, "reorder", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Menus.Resource, SystemPermissionCatalog.Menus.Update))
            .MapGet(GetUserMenus, "getUserMenus")
            .MapGet(MenuGetById, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Menus.Resource, SystemPermissionCatalog.Menus.List))
            .MapPost(MenuGetPagedList, "pagedList", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Menus.Resource, SystemPermissionCatalog.Menus.List))
            .MapPost(MenuCreate, configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Menus.Resource, SystemPermissionCatalog.Menus.Create))
            .MapPut(MenuUpdate, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Menus.Resource, SystemPermissionCatalog.Menus.Update))
            .MapDelete(MenuDelete, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Menus.Resource, SystemPermissionCatalog.Menus.Delete));
    }
    public async Task ReorderMenus(IDispatcher sender,MenuReorderCommand request,CancellationToken ct)
    {
        await sender.Send(request, ct);
    }
    public  async Task<Results<NoContent, BadRequest>> MenuDelete([FromServices] IDispatcher sender, Guid id)
    {
        await sender.Send(new MenuDeleteCommand(id));
        return TypedResults.NoContent();
    }
    public  async Task<Results<NoContent, BadRequest>> MenuUpdate(IDispatcher sender, Guid id, MenuUpdateCommand request)
    {
        request.Id = id;
        await sender.Send(request);
        return TypedResults.NoContent();
    }
    public  async Task<Results<Ok<Guid>, BadRequest>> MenuCreate(IDispatcher sender, MenuCreateCommand request)
    {
        var res = await sender.Send(request);
        return TypedResults.Ok(res);
    }
    public  async Task<Results<Ok<PagedList<MenuPagedListDto>>, BadRequest>> MenuGetPagedList(IDispatcher sender, MenuPagedListQuery request)
    {
        var res = await sender.Query(request);
        return TypedResults.Ok(res);
    }
    public   async Task<Results<Ok<MenuGetByIdDto>, NotFound>> MenuGetById([FromServices]IDispatcher sender, Guid id, CancellationToken cancellationToken = default)
    {
        var res = await sender.Query(new MenuGetByIdQuery(id),cancellationToken);
        return TypedResults.Ok(res);
    }
    public  async Task<Results<Ok<List<MenuCategoryDto>>, BadRequest>> GetUserMenus(IDispatcher sender, [AsParameters] GetUserMenusQuery parameter, CancellationToken cancellationToken = default)
    {
        var res = await sender.Query(parameter, cancellationToken);
        return TypedResults.Ok(res);
    }




}