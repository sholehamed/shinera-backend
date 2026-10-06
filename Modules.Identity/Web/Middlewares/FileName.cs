using Application.SharedKernel.Exceptions;
using Modules.System.Identity.Application.Abstractions;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace Modules.System.Identity.Web.Middlewares;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        ITenantContext tenantContext,
        ITenantAccessResolver tenantResolver,
        IBranchAccessResolver branchResolver)
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

        var tenantScope = await tenantResolver.ResolveAsync(
            userId,
            httpContext.RequestAborted);

        Guid? activeTenantId = null;

        if (httpContext.Request.Headers.TryGetValue(
                "X-Tenant-Id",
                out var rawTenantSelector))
        {
            if (!Guid.TryParse(rawTenantSelector, out var requestedTenantId) ||
                !tenantScope.Readable.Contains(requestedTenantId))
            {
                throw new TenantAccessException(
                    "tenant.access_denied",
                    "The selected tenant is not available to the current user.");
            }

            activeTenantId = requestedTenantId;
        }
        else if (tenantScope.Readable.Length == 1)
        {
            activeTenantId = tenantScope.Readable[0];
        }

        Guid[] readableBranches = [];
        Guid[] writableBranches = [];
        Guid? activeBranchId = null;

        if (activeTenantId.HasValue)
        {
            var branchScope = await branchResolver.ResolveAsync(
                userId,
                activeTenantId.Value,
                httpContext.RequestAborted);

            readableBranches = branchScope.Readable;
            writableBranches = branchScope.Writable;

            if (httpContext.Request.Headers.TryGetValue(
                    "X-Branch-Id",
                    out var rawBranchSelector))
            {
                if (!Guid.TryParse(rawBranchSelector, out var requestedBranchId) ||
                    !readableBranches.Contains(requestedBranchId))
                {
                    throw new TenantAccessException(
                        "branch.access_denied",
                        "The selected branch is not available to the current user.");
                }

                activeBranchId = requestedBranchId;
            }
            else if (readableBranches.Length == 1)
            {
                activeBranchId = readableBranches[0];
            }
        }
        else if (httpContext.Request.Headers.ContainsKey("X-Branch-Id"))
        {
            throw new TenantAccessException(
                "branch.tenant_context_missing",
                "Select a tenant before selecting a branch.");
        }

        tenantContext.Initialize(
            userId,
            false,
            tenantScope.Readable,
            tenantScope.Writable,
            activeTenantId,
            readableBranches,
            writableBranches,
            activeBranchId);

        await next(httpContext);
    }
}
