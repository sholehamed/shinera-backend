using Modules.System.Identity.Application.Authorization;
using Modules.System.Workforce.Application.Features.Staff;
using Web.SharedKernel.Authorization;
using Web.SharedKernel.Models;

namespace Modules.System.Workforce.Web.Endpoints;

public sealed class Staff : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(
                List,
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Staff.Resource,
                        SystemPermissionCatalog.Staff.View))
            .MapGet(
                Details,
                "{id}",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Staff.Resource,
                        SystemPermissionCatalog.Staff.View))
            .MapPost(
                Create,
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Staff.Resource,
                        SystemPermissionCatalog.Staff.Create))
            .MapPut(
                Update,
                "{id}",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Staff.Resource,
                        SystemPermissionCatalog.Staff.Update))
            .MapPatch(
                SetActive,
                "{id}/state",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Staff.Resource,
                        SystemPermissionCatalog.Staff.Update))
            .MapPut(
                ReplaceBranches,
                "{id}/branches",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Staff.Resource,
                        SystemPermissionCatalog.Staff.AssignBranches))
            .MapPut(
                ReplaceServices,
                "{id}/services",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Staff.Resource,
                        SystemPermissionCatalog.Staff.AssignServices));
    }

    public async Task<Ok<PagedList<StaffListItemDto>>> List(
        IDispatcher dispatcher,
        [AsParameters] StaffListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Query(
            query,
            cancellationToken);

        return TypedResults.Ok(result);
    }

    public async Task<Ok<StaffDetailsDto>> Details(
        IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Query(
            new StaffDetailsQuery(id),
            cancellationToken);

        return TypedResults.Ok(result);
    }

    public async Task<Created<Guid>> Create(
        IDispatcher dispatcher,
        CreateStaffCommand command,
        CancellationToken cancellationToken)
    {
        var id = await dispatcher.Send(
            command,
            cancellationToken);

        return TypedResults.Created(
            $"/Workforce/Staff/{id}",
            id);
    }

    public async Task<NoContent> Update(
        IDispatcher dispatcher,
        Guid id,
        UpdateStaffRequest request,
        CancellationToken cancellationToken)
    {
        await dispatcher.Send(
            new UpdateStaffCommand(
                id,
                request.FirstName,
                request.LastName,
                request.Phone,
                request.Email,
                request.IsActive),
            cancellationToken);

        return TypedResults.NoContent();
    }

    public async Task<NoContent> SetActive(
        IDispatcher dispatcher,
        Guid id,
        StaffStateRequest request,
        CancellationToken cancellationToken)
    {
        await dispatcher.Send(
            new SetStaffActiveCommand(
                id,
                request.IsActive),
            cancellationToken);

        return TypedResults.NoContent();
    }

    public async Task<
        Results<
            NoContent,
            BadRequest<ApiResponse>>> ReplaceBranches(
        IDispatcher dispatcher,
        Guid id,
        StaffBranchesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new ReplaceStaffBranchesCommand(
                id,
                request.BranchIds),
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

    public async Task<
        Results<
            NoContent,
            BadRequest<ApiResponse>>> ReplaceServices(
        IDispatcher dispatcher,
        Guid id,
        StaffServicesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new ReplaceStaffServicesCommand(
                id,
                request.ServiceIds),
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

    public sealed record UpdateStaffRequest(
        string FirstName,
        string LastName,
        string Phone,
        string Email,
        bool IsActive);

    public sealed record StaffStateRequest(
        bool IsActive);

    public sealed record StaffBranchesRequest(
        IReadOnlyCollection<Guid> BranchIds);

    public sealed record StaffServicesRequest(
        IReadOnlyCollection<Guid> ServiceIds);
}
