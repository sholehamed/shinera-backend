using Modules.System.Identity.Application.Authorization;
using Modules.System.Services.Application.Features.Services;
using Web.SharedKernel.Authorization;
using Web.SharedKernel.Models;

namespace Modules.System.Services.Web.Endpoints;

public sealed class Services : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(
                List,
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Services.Resource,
                        SystemPermissionCatalog.Services.View))
            .MapGet(
                Details,
                "{id}",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Services.Resource,
                        SystemPermissionCatalog.Services.View))
            .MapPost(
                Create,
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Services.Resource,
                        SystemPermissionCatalog.Services.Create))
            .MapPut(
                Update,
                "{id}",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Services.Resource,
                        SystemPermissionCatalog.Services.Update))
            .MapPatch(
                SetActive,
                "{id}/state",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Services.Resource,
                        SystemPermissionCatalog.Services.Update));
    }

    public async Task<Ok<PagedList<ServiceListItemDto>>> List(
        IDispatcher dispatcher,
        [AsParameters] ServiceListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Query(
            query,
            cancellationToken);

        return TypedResults.Ok(result);
    }

    public async Task<Ok<ServiceDetailsDto>> Details(
        IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Query(
            new ServiceDetailsQuery(id),
            cancellationToken);

        return TypedResults.Ok(result);
    }

    public async Task<
        Results<
            Created<Guid>,
            BadRequest<ApiResponse>>> Create(
        IDispatcher dispatcher,
        CreateServiceCommand command,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(
                ApiResponse.Fail(
                    result.Error.Code,
                    result.Error.Message));
        }

        return TypedResults.Created(
            $"/Services/Services/{result.Value}",
            result.Value);
    }

    public async Task<
        Results<
            NoContent,
            BadRequest<ApiResponse>>> Update(
        IDispatcher dispatcher,
        Guid id,
        UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new UpdateServiceCommand(
                id,
                request.CategoryId,
                request.Name,
                request.Description,
                request.DurationMinutes,
                request.Price,
                request.IsActive),
            cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(
                ApiResponse.Fail(
                    result.Error.Code,
                    result.Error.Message));
        }

        return TypedResults.NoContent();
    }

    public async Task<NoContent> SetActive(
        IDispatcher dispatcher,
        Guid id,
        ServiceStateRequest request,
        CancellationToken cancellationToken)
    {
        await dispatcher.Send(
            new SetServiceActiveCommand(
                id,
                request.IsActive),
            cancellationToken);

        return TypedResults.NoContent();
    }

    public sealed record UpdateServiceRequest(
        Guid CategoryId,
        string Name,
        string? Description,
        int DurationMinutes,
        decimal Price,
        bool IsActive);

    public sealed record ServiceStateRequest(
        bool IsActive);
}
