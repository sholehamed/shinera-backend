using Modules.System.Identity.Application.Features.Tenants.Commands;
using Modules.System.Identity.Application.Authorization;
using Web.SharedKernel.Authorization;
using Modules.System.Identity.Application.Features.Users.Commands;
using Modules.System.Identity.Application.Features.Users.Queries;


namespace Modules.System.Identity.Web.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapPost(UserPagedList, "pagedList", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Users.Resource, SystemPermissionCatalog.Users.List))
            .MapGet(UserGetById, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Users.Resource, SystemPermissionCatalog.Users.List))
            .MapGet(IsUsernameTaken, "isUserNameTaken", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Users.Resource, SystemPermissionCatalog.Users.List))
            .MapPost(UserCreate, configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Users.Resource, SystemPermissionCatalog.Users.Create))
            .MapPut(UserUpdate, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Users.Resource, SystemPermissionCatalog.Users.Update))
            .MapDelete(UserDelete, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Users.Resource, SystemPermissionCatalog.Users.Delete))
            .MapPatch(UserChangeState, "{id}", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Users.Resource, SystemPermissionCatalog.Users.Update))
            .MapPatch(UserChangeLockState, "{id}/changeLock", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Users.Resource, SystemPermissionCatalog.Users.Update));
    }

    public async Task UserDelete(IDispatcher sender, Guid id)
    {
        await sender.Send(new UserDeleteCommand(id));
    }
    public async Task<Results<Ok<bool>, BadRequest>> IsUsernameTaken(IDispatcher sender, [AsParameters] IsUsernameTakenQuery parameter, CancellationToken cancellationToken = default)
    {
        var res = await sender.Query(parameter, cancellationToken);
        return TypedResults.Ok(res);
    }
    public async Task<Results<Ok<PagedList<UserPagedListDto>>, BadRequest>> UserPagedList(IDispatcher sender, UserPagedListQuery parameter, CancellationToken cancellationToken = default)
    {
        var res = await sender.Query(parameter, cancellationToken);
        return TypedResults.Ok(res);
    }

    public async Task<Results<Ok<UserGetByIdDto>, NotFound>> UserGetById([FromServices] IDispatcher sender, Guid id, CancellationToken cancellationToken = default)
    {
        var res = await sender.Query(new UserGetByIdQuery(id), cancellationToken);
        return TypedResults.Ok(res);
    }
    public async Task<Results<Created<Guid>, BadRequest>> UserCreate(IDispatcher sender, UserCreateCommand parameter, CancellationToken cancellationToken = default)
    {
        var res = await sender.Send(parameter, cancellationToken);
        return TypedResults.Created(res.ToString(), res);
    }
    public async Task<NoContent> UserUpdate([FromServices] IDispatcher sender, [FromRoute] Guid id, UserUpdateCommand parameter, CancellationToken cancellationToken = default)
    {
        parameter.Id = id;
        await sender.Send(parameter, cancellationToken);
        return TypedResults.NoContent();
    }
    public async Task UserChangeState(IDispatcher sender, Guid id)
    {
        await sender.Send(new UserChangeStateCommand(id));
    }
    public async Task UserChangeLockState(IDispatcher sender, Guid id)
    {
        await sender.Send(new UserChangeLockStateCommand(id));
    }


}