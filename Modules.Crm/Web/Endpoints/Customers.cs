using Modules.System.Crm.Application.Features.Customers;
using Modules.System.Identity.Application.Authorization;
using Web.SharedKernel.Authorization;
using Web.SharedKernel.Models;

namespace Modules.System.Crm.Web.Endpoints;

public sealed class Customers : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(
                Search,
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Customers.Resource,
                        SystemPermissionCatalog.Customers.View))
            .MapGet(
                Details,
                "{id}",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Customers.Resource,
                        SystemPermissionCatalog.Customers.View))
            .MapPost(
                Create,
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Customers.Resource,
                        SystemPermissionCatalog.Customers.Create))
            .MapPost(
                AddNote,
                "{id}/notes",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Customers.Resource,
                        SystemPermissionCatalog.Customers.AddNote));
    }

    public async Task<Ok<PagedList<CustomerListItemDto>>> Search(
        IDispatcher dispatcher,
        [AsParameters] CustomerSearchQuery query,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Query(
            query,
            cancellationToken);

        return TypedResults.Ok(result);
    }

    public async Task<Ok<CustomerDetailsDto>> Details(
        IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Query(
            new CustomerDetailsQuery(id),
            cancellationToken);

        return TypedResults.Ok(result);
    }

    public async Task<
        Results<
            Created<Guid>,
            BadRequest<ApiResponse>,
            Conflict<ApiResponse>>> Create(
        IDispatcher dispatcher,
        CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            var response = ApiResponse.Fail(
                result.Error.Code,
                result.Error.Message);

            if (result.Error.Type ==
                ErrorType.Conflict)
            {
                return TypedResults.Conflict(response);
            }

            return TypedResults.BadRequest(response);
        }

        return TypedResults.Created(
            $"/Crm/Customers/{result.Value}",
            result.Value);
    }

    public async Task<
        Results<
            Created<Guid>,
            BadRequest<ApiResponse>,
            NotFound<ApiResponse>>> AddNote(
        IDispatcher dispatcher,
        Guid id,
        CustomerNoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(
            new AddCustomerNoteCommand(
                id,
                request.Content),
            cancellationToken);

        if (result.IsFailure)
        {
            var response = ApiResponse.Fail(
                result.Error.Code,
                result.Error.Message);

            if (result.Error.Type ==
                ErrorType.NotFound)
            {
                return TypedResults.NotFound(response);
            }

            return TypedResults.BadRequest(response);
        }

        return TypedResults.Created(
            $"/Crm/Customers/{id}/notes/{result.Value}",
            result.Value);
    }

    public sealed record CustomerNoteRequest(
        string Content);
}
