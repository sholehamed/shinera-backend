using Modules.System.Appointments.Application.Features.Appointments;
using Modules.System.Identity.Application.Authorization;
using Web.SharedKernel.Authorization;
using Web.SharedKernel.Models;

namespace Modules.System.Appointments.Web.Endpoints;

public sealed class Appointments : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(
                AvailableSlots,
                "available-slots",
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Appointments.Resource,
                        SystemPermissionCatalog.Appointments.View))
            .MapPost(
                Create,
                configure: endpoint =>
                    endpoint.RequirePermission(
                        SystemPermissionCatalog.Appointments.Resource,
                        SystemPermissionCatalog.Appointments.Create));
    }

    public async Task<
        Results<
            Ok<IReadOnlyList<TimeOnly>>,
            BadRequest<ApiResponse>>> AvailableSlots(
        IDispatcher dispatcher,
        [AsParameters] AvailableSlotsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Query(
            query,
            cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(
                ApiResponse.Fail(
                    result.Error.Code,
                    result.Error.Message));
        }

        return TypedResults.Ok(result.Value);
    }

    public async Task<
        Results<
            Created<Guid>,
            BadRequest<ApiResponse>,
            Conflict<ApiResponse>>> Create(
        IDispatcher dispatcher,
        CreateAppointmentCommand command,
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

            if (result.Error.Type == ErrorType.Conflict)
            {
                return TypedResults.Conflict(response);
            }

            return TypedResults.BadRequest(response);
        }

        return TypedResults.Created(
            $"/Appointments/Appointments/{result.Value}",
            result.Value);
    }
}
