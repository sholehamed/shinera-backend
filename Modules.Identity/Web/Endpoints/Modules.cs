using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Application.Features.Modules.Commands;
using Modules.System.Identity.Application.Features.Modules.Queries;
using Web.SharedKernel.Authorization;

namespace Modules.System.Identity.Web.Endpoints;

public class Modules : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapPost(
                ModulePagedList,
                "pagedList",
                configure: x => x.RequirePermission(
                    SystemPermissionCatalog.Modules.Resource,
                    SystemPermissionCatalog.Modules.List))
            .MapGet(
                ModuleGetById,
                "{id}",
                configure: x => x.RequirePermission(
                    SystemPermissionCatalog.Modules.Resource,
                    SystemPermissionCatalog.Modules.List))
            .MapGet(
                ModuleLookup,
                "lookup",
                configure: x => x.RequirePermission(
                    SystemPermissionCatalog.Modules.Resource,
                    SystemPermissionCatalog.Modules.List))
            .MapPost(
                ModuleCreate,
                configure: x => x.RequirePermission(
                    SystemPermissionCatalog.Modules.Resource,
                    SystemPermissionCatalog.Modules.Create))
            .MapPut(
                ModuleUpdate,
                "{id}",
                configure: x => x.RequirePermission(
                    SystemPermissionCatalog.Modules.Resource,
                    SystemPermissionCatalog.Modules.Update))
            .MapDelete(
                ModuleDelete,
                "{id}",
                configure: x => x.RequirePermission(
                    SystemPermissionCatalog.Modules.Resource,
                    SystemPermissionCatalog.Modules.Delete))
            .MapPatch(
                ModuleChangeState,
                "{id}",
                configure: x => x.RequirePermission(
                    SystemPermissionCatalog.Modules.Resource,
                    SystemPermissionCatalog.Modules.Update));
    }

    public async Task ModuleChangeState(IDispatcher sender, Guid id)
    {
        await sender.Send(new ModuleChangeStateCommand(id));
    }

    public async Task ModuleDelete(IDispatcher sender, Guid id)
    {
        await sender.Send(new ModuleDeleteCommand(id));
    }

    public async Task<Results<Ok<List<LookupDto>>, BadRequest>> ModuleLookup(
        IDispatcher sender,
        string? text,
        CancellationToken cancellationToken = default)
    {
        var res = await sender.Query(
            new ModuleLookupQuery(text),
            cancellationToken);

        return TypedResults.Ok(res);
    }

    public async Task<Results<Ok<PagedList<ModulePagedListDto>>, BadRequest>>
        ModulePagedList(
            IDispatcher sender,
            ModulePagedListQuery parameter,
            CancellationToken cancellationToken = default)
    {
        var res = await sender.Query(parameter, cancellationToken);
        return TypedResults.Ok(res);
    }

    public async Task<Results<Ok<ModuleGetByIdDto>, NotFound>> ModuleGetById(
        [FromServices] IDispatcher sender,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var res = await sender.Query(
            new ModuleGetByIdQuery(id),
            cancellationToken);

        return TypedResults.Ok(res);
    }

    public async Task<Results<Created<Guid>, BadRequest>> ModuleCreate(
        IDispatcher sender,
        ModuleCreateCommand parameter,
        CancellationToken cancellationToken = default)
    {
        var res = await sender.Send(parameter, cancellationToken);
        return TypedResults.Created(res.ToString(), res);
    }

    public async Task<NoContent> ModuleUpdate(
        [FromServices] IDispatcher sender,
        [FromRoute] Guid id,
        ModuleUpdateCommand parameter,
        CancellationToken cancellationToken = default)
    {
        parameter.Id = id;
        await sender.Send(parameter, cancellationToken);
        return TypedResults.NoContent();
    }
}
