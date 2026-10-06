using Modules.System.Identity.Application.Authorization;
using Modules.System.Services.Application.Features.ServiceCategories;
using Web.SharedKernel.Authorization;
using Web.SharedKernel.Models;

namespace Modules.System.Services.Web.Endpoints;

public sealed class ServiceCategories : EndpointGroupBase
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
            .MapDelete(
                Delete,
                "{id}",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Services.Resource,
                        SystemPermissionCatalog.Services.Delete));
    }

    public async Task<Ok<IReadOnlyList<ServiceCategoryDto>>> List(
        IDispatcher dispatcher,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Query(
            new ServiceCategoryListQuery(isActive),
            cancellationToken);

        return TypedResults.Ok(result);
    }

    public async Task<Ok<ServiceCategoryDto>> Details(
        IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Query(
            new ServiceCategoryDetailsQuery(id),
            cancellationToken);

        return TypedResults.Ok(result);
    }

    public async Task<Created<Guid>> Create(
        IDispatcher dispatcher,
        CreateServiceCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var id = await dispatcher.Send(
            command,
            cancellationToken);

        return TypedResults.Created(
            $"/Services/ServiceCategories/{id}",
            id);
    }

    public async Task<NoContent> Update(
        IDispatcher dispatcher,
        Guid id,
        UpdateServiceCategoryRequest request,
        CancellationToken cancellationToken)
    {
        await dispatcher.Send(
            new UpdateServiceCategoryCommand(
                id,
                request.Name,
                request.Description,
                request.SortOrder,
                request.IsActive),
            cancellationToken);

        return TypedResults.NoContent();
    }

    public async Task<Results<NoContent, Conflict<ApiResponse>>> Delete(
        IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new DeleteServiceCategoryCommand(id),
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

    public sealed record UpdateServiceCategoryRequest(
        string Name,
        string? Description,
        int SortOrder,
        bool IsActive);
}
