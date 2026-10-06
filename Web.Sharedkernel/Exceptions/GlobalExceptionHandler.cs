using Application.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Web.SharedKernel.Models;

namespace Web.SharedKernel.Exceptions;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                ApiResponse.Fail(
                    "validation.failed",
                    "One or more validation errors occurred.",
                    validationException.Errors)),

            TenantAccessException tenantException => (
                StatusCodes.Status403Forbidden,
                ApiResponse.Fail(
                    tenantException.Code,
                    tenantException.Message)),

            ForbiddenAccessException => (
                StatusCodes.Status403Forbidden,
                ApiResponse.Fail(
                    "authorization.forbidden",
                    "You do not have permission to perform this action.")),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                ApiResponse.Fail(
                    "authentication.unauthorized",
                    "Authentication is required.")),

            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                ApiResponse.Fail(
                    "common.not_found",
                    "The requested resource was not found.")),

            ServiceExeption serviceException => (
                StatusCodes.Status400BadRequest,
                ApiResponse.Fail(
                    $"legacy.business_error.{serviceException.ErrorCode}",
                    serviceException.Message)),

            _ => (
                StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(
                    "common.unexpected_error",
                    "An unexpected error occurred.",
                    new { traceId = httpContext.TraceIdentifier }))
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", httpContext.TraceIdentifier);
        else
            logger.LogWarning(exception, "Request failed with status {StatusCode}. TraceId: {TraceId}", statusCode, httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
