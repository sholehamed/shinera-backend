using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Web.SharedKernel.Models;

namespace Web.SharedKernel.Authorization;

public sealed class StableAuthorizationMiddlewareResultHandler
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
                    message.StartsWith("authorization.", StringComparison.Ordinal) ||
                    message.StartsWith("subscription.", StringComparison.Ordinal));

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;

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

            "authorization.user_inactive" =>
                "The current user account is inactive.",

            "subscription.required" =>
                "An active subscription is required for this operation.",

            "subscription.inactive" =>
                "The current subscription is not entitled to this operation.",

            "subscription.feature_unavailable" =>
                "The current subscription does not include this feature.",

            "subscription.limit_reached" =>
                "The subscription limit for this operation has been reached.",

            "subscription.over_limit" =>
                "Current usage exceeds the subscription limit.",

            _ =>
                "The required permission is not granted."
        };
}
