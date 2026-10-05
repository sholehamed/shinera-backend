using Application.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Web.Middlewares;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace Application.Tests.Tenancy;

public sealed class TenantResolutionMiddlewareTests
{
    [Fact]
    public async Task SingleMembership_WithoutSelector_BecomesActiveTenant()
    {
        var tenantId = Guid.NewGuid();
        var context = CreateAuthenticatedContext();
        var tenantContext = new TenantContext();
        var resolver = new StubResolver([tenantId]);

        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(
            context,
            tenantContext,
            resolver,
            new MemoryCache(new MemoryCacheOptions()));

        Assert.Equal(tenantId, tenantContext.ActiveTenantId);
    }

    [Fact]
    public async Task MultipleMemberships_WithoutSelector_DoesNotGuessActiveTenant()
    {
        var context = CreateAuthenticatedContext();
        var tenantContext = new TenantContext();
        var resolver = new StubResolver([Guid.NewGuid(), Guid.NewGuid()]);

        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(
            context,
            tenantContext,
            resolver,
            new MemoryCache(new MemoryCacheOptions()));

        Assert.Null(tenantContext.ActiveTenantId);
    }

    [Fact]
    public async Task SelectorOutsideMembership_IsRejected()
    {
        var allowedTenant = Guid.NewGuid();
        var context = CreateAuthenticatedContext();
        context.Request.Headers["X-Tenant-Id"] = Guid.NewGuid().ToString();

        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        var exception = await Assert.ThrowsAsync<TenantAccessException>(
            () => middleware.InvokeAsync(
                context,
                new TenantContext(),
                new StubResolver([allowedTenant]),
                new MemoryCache(new MemoryCacheOptions())));

        Assert.Equal("tenant.access_denied", exception.Code);
    }

    [Fact]
    public async Task ValidSelector_IsAccepted()
    {
        var selectedTenant = Guid.NewGuid();
        var context = CreateAuthenticatedContext();
        context.Request.Headers["X-Tenant-Id"] = selectedTenant.ToString();

        var tenantContext = new TenantContext();
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(
            context,
            tenantContext,
            new StubResolver([Guid.NewGuid(), selectedTenant]),
            new MemoryCache(new MemoryCacheOptions()));

        Assert.Equal(selectedTenant, tenantContext.ActiveTenantId);
    }

    private static DefaultHttpContext CreateAuthenticatedContext()
    {
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
            [new Claim(OpenIddictConstants.Claims.Subject, userId.ToString())],
            "test");

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
    }

    private sealed class StubResolver(Guid[] tenantIds) : ITenantAccessResolver
    {
        public Task<(Guid[] Readable, Guid[] Writable)> ResolveAsync(
            Guid userId,
            bool isSuperAdmin,
            CancellationToken cancellationToken)
        {
            return Task.FromResult((tenantIds, tenantIds));
        }
    }
}
