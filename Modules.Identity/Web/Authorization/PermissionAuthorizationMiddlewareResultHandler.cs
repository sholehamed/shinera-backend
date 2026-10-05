using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Web.SharedKernel.Models;

namespace Modules.System.Identity.Web.Authorization;

public sealed class PermissionAuthorizationMiddlewareResultHandler
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            var errorCode = authorizeResult.AuthorizationFailure?
                .FailureReasons
                .Select(reason => reason.Message)
                .FirstOrDefault(message =>
                    message.StartsWith(
                        "authorization.",
                        StringComparison.Ordinal));

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                await context.Response.WriteAsJsonAsync(
                    ApiResponse.Fail(
                        errorCode,
                        GetMessage(errorCode)),
                    context.RequestAborted);

                return;
            }
        }

        await _defaultHandler.HandleAsync(
            next,
            context,
            policy,
            authorizeResult);
    }

    private static string GetMessage(string errorCode) =>
        errorCode switch
        {
            "authorization.tenant_denied" =>
                "The selected tenant is not available to the current user.",

            "authorization.scope_denied" =>
                "The requested operation is outside the allowed permission scope.",

            _ =>
                "The required permission is not granted."
        };
}
