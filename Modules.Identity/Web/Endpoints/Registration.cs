using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Modules.System.Identity.Application.Features.Registration.Commands;
using Modules.System.Identity.Web.Authentication;
using OpenIddict.Abstractions;
using System.Security.Claims;
using Web.SharedKernel.Models;

namespace Modules.System.Identity.Web.Endpoints;

public sealed class Registration : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapPost(
                Register,
                configure: endpoint => endpoint
                    .AllowAnonymous()
                    .RequireRateLimiting(
                        "shinera-auth-registration"));
    }

    public async Task<
        Results<
            Ok<ApiResponse<RegistrationSuccessDto>>,
            BadRequest<ApiResponse>,
            Conflict<ApiResponse>>>
        Register(
            IDispatcher dispatcher,
            RegisterWorkspaceCommand command,
            HttpContext httpContext,
            TimeProvider timeProvider,
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
                return TypedResults.Conflict(response);

            return TypedResults.BadRequest(response);
        }

        var registration = result.Value;

        var identity = new ClaimsIdentity(
            InteractiveAuthenticationDefaults.Scheme,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        identity.AddClaim(new Claim(
            OpenIddictConstants.Claims.Subject,
            registration.UserId.ToString()));

        identity.AddClaim(new Claim(
            OpenIddictConstants.Claims.Name,
            registration.UserName));

        identity.AddClaim(new Claim(
            OpenIddictConstants.Claims.Email,
            registration.Email));

        await httpContext.SignInAsync(
            InteractiveAuthenticationDefaults.Scheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true,
                ExpiresUtc =
                    timeProvider.GetUtcNow()
                        .AddHours(8)
            });

        return TypedResults.Ok(
            ApiResponse<RegistrationSuccessDto>.Ok(
                new RegistrationSuccessDto(
                    registration.UserId,
                    registration.TenantId,
                    registration.TenantSlug,
                    registration.BranchId,
                    registration.BusinessProfileId,
                    registration.SubscriptionId,
                    registration.PlanKey,
                    "/onboarding")));
    }

    public sealed record RegistrationSuccessDto(
        Guid UserId,
        Guid TenantId,
        string TenantSlug,
        Guid BranchId,
        Guid BusinessProfileId,
        Guid SubscriptionId,
        string PlanKey,
        string Next);
}
