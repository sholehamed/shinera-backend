using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Application.Features.Branches.Commands;
using Modules.System.Identity.Application.Features.Branches.Queries;
using Web.SharedKernel.Authorization;
using Web.SharedKernel.Models;

namespace Modules.System.Identity.Web.Endpoints;

public sealed class Branches : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(
                List,
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Branches.Resource,
                        SystemPermissionCatalog.Branches.List))
            .MapGet(
                Details,
                "{id}",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Branches.Resource,
                        SystemPermissionCatalog.Branches.List))
            .MapPost(
                Create,
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Branches.Resource,
                        SystemPermissionCatalog.Branches.Create))
            .MapPut(
                Update,
                "{id}",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Branches.Resource,
                        SystemPermissionCatalog.Branches.Update))
            .MapPatch(
                Disable,
                "{id}/disable",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Branches.Resource,
                        SystemPermissionCatalog.Branches.Disable))
            .MapPut(
                SetMain,
                "{id}/main",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Branches.Resource,
                        SystemPermissionCatalog.Branches.SetMain));
    }

    public async Task<Ok<IReadOnlyList<BranchListItemDto>>> List(
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Query(
            new BranchListQuery(),
            cancellationToken);

        return TypedResults.Ok(result);
    }

    public async Task<Ok<BranchDetailsDto>> Details(
        IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Query(
            new BranchGetByIdQuery(id),
            cancellationToken);

        return TypedResults.Ok(result);
    }

    public async Task<Created<Guid>> Create(
        IDispatcher dispatcher,
        BranchCreateCommand command,
        CancellationToken cancellationToken)
    {
        var id = await dispatcher.Send(
            command,
            cancellationToken);

        return TypedResults.Created(
            $"/System/Branches/{id}",
            id);
    }

    public async Task<NoContent> Update(
        IDispatcher dispatcher,
        Guid id,
        BranchUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await dispatcher.Send(
            new BranchUpdateCommand(
                id,
                request.Name,
                request.Phone,
                request.Address),
            cancellationToken);

        return TypedResults.NoContent();
    }

    public async Task<Results<NoContent, Conflict<ApiResponse>>> Disable(
        IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new BranchDisableCommand(id),
            cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.Conflict(
                ApiResponse.Fail(
                    result.Error.Code,
                    result.Error.Message));
        }

        return TypedResults.NoContent();
    }

    public async Task<Results<NoContent, Conflict<ApiResponse>>> SetMain(
        IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new SetMainBranchCommand(id),
            cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.Conflict(
                ApiResponse.Fail(
                    result.Error.Code,
                    result.Error.Message));
        }

        return TypedResults.NoContent();
    }

    public sealed record BranchUpdateRequest(
        string Name,
        string? Phone,
        string? Address);
}
