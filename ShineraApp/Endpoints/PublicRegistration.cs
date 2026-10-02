using ShineraApp.Application.Features.Registration;
using Web.Sharedkernel.Util;
namespace ShineraApp.Endpoints;
public sealed class PublicRegistration : EndpointGroupBase
{
    public override void Map(WebApplication app) => app.MapPost("/api/public/registrations", Register)
        .AllowAnonymous().RequireRateLimiting("registration").WithTags("Public registration")
        .Produces<ApiResponse<RegistrationReceipt>>()
        .Produces<ApiResponse<RegistrationReceipt>>(400).Produces<ApiResponse<RegistrationReceipt>>(409);
    public async Task<IResult> Register(RegisterWorkspaceCommand command, IDispatcher dispatcher, CancellationToken ct)
        => (await dispatcher.Send(command, ct)).ToHttpResult();
}
