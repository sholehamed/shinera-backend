using Application.SharedKernel.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Modules.System.Identity.Application.Abstractions;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace Modules.System.Identity.Web.Middlewares;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        ITenantContext tenantContext,
        ITenantAccessResolver resolver,
        IMemoryCache cache)
    {
        var principal = httpContext.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            await next(httpContext);
            return;
        }

        var subject = principal.FindFirstValue(OpenIddictConstants.Claims.Subject);
        if (!Guid.TryParse(subject, out var userId))
        {
            throw new TenantAccessException(
                "tenant.user_context_invalid",
                "The authenticated user context is invalid.");
        }

        var version = await cache.GetOrCreateAsync(
            $"tenant-access-ver:{userId}",
            _ => Task.FromResult(0L));

        var scope = await cache.GetOrCreateAsync(
            $"tenant-access:{userId}:{version}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                return await resolver.ResolveAsync(
                    userId,
                    httpContext.RequestAborted);
            });

        Guid? activeTenantId = null;

        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var rawSelector))
        {
            if (!Guid.TryParse(rawSelector, out var requestedTenantId) ||
                !scope.Readable.Contains(requestedTenantId))
            {
                throw new TenantAccessException(
                    "tenant.access_denied",
                    "The selected tenant is not available to the current user.");
            }

            activeTenantId = requestedTenantId;
        }
        else if (scope.Readable.Length == 1)
        {
            activeTenantId = scope.Readable[0];
        }

        tenantContext.Initialize(
            userId,
            false,
            scope.Readable,
            scope.Writable,
            activeTenantId);

        await next(httpContext);
    }
}
