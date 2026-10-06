using Application.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
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
        var tenantContext = new TenantContext();

        await InvokeAsync(
            CreateAuthenticatedContext(),
            tenantContext,
            [tenantId],
            []);

        Assert.Equal(tenantId, tenantContext.ActiveTenantId);
        Assert.Null(tenantContext.ActiveBranchId);
    }

    [Fact]
    public async Task MultipleMemberships_WithoutSelector_DoesNotGuessActiveTenant()
    {
        var tenantContext = new TenantContext();

        await InvokeAsync(
            CreateAuthenticatedContext(),
            tenantContext,
            [Guid.NewGuid(), Guid.NewGuid()],
            []);

        Assert.Null(tenantContext.ActiveTenantId);
        Assert.Null(tenantContext.ActiveBranchId);
    }

    [Fact]
    public async Task TenantSelectorOutsideMembership_IsRejected()
    {
        var context = CreateAuthenticatedContext();
        context.Request.Headers["X-Tenant-Id"] =
            Guid.NewGuid().ToString();

        var exception = await Assert.ThrowsAsync<TenantAccessException>(
            () => InvokeAsync(
                context,
                new TenantContext(),
                [Guid.NewGuid()],
                []));

        Assert.Equal("tenant.access_denied", exception.Code);
    }

    [Fact]
    public async Task SingleAccessibleBranch_WithoutSelector_BecomesActiveBranch()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var tenantContext = new TenantContext();

        await InvokeAsync(
            CreateAuthenticatedContext(),
            tenantContext,
            [tenantId],
            [branchId]);

        Assert.Equal(tenantId, tenantContext.ActiveTenantId);
        Assert.Equal(branchId, tenantContext.ActiveBranchId);
    }

    [Fact]
    public async Task MultipleAccessibleBranches_WithoutSelector_DoesNotGuessActiveBranch()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = new TenantContext();

        await InvokeAsync(
            CreateAuthenticatedContext(),
            tenantContext,
            [tenantId],
            [Guid.NewGuid(), Guid.NewGuid()]);

        Assert.Equal(tenantId, tenantContext.ActiveTenantId);
        Assert.Null(tenantContext.ActiveBranchId);
    }

    [Fact]
    public async Task BranchSelectorOutsideMembership_IsRejected()
    {
        var tenantId = Guid.NewGuid();
        var context = CreateAuthenticatedContext();
        context.Request.Headers["X-Branch-Id"] =
            Guid.NewGuid().ToString();

        var exception = await Assert.ThrowsAsync<TenantAccessException>(
            () => InvokeAsync(
                context,
                new TenantContext(),
                [tenantId],
                [Guid.NewGuid()]));

        Assert.Equal("branch.access_denied", exception.Code);
    }

    [Fact]
    public async Task BranchSelectorWithoutActiveTenant_IsRejected()
    {
        var context = CreateAuthenticatedContext();
        context.Request.Headers["X-Branch-Id"] =
            Guid.NewGuid().ToString();

        var exception = await Assert.ThrowsAsync<TenantAccessException>(
            () => InvokeAsync(
                context,
                new TenantContext(),
                [Guid.NewGuid(), Guid.NewGuid()],
                []));

        Assert.Equal(
            "branch.tenant_context_missing",
            exception.Code);
    }

    [Fact]
    public async Task ValidTenantAndBranchSelectors_AreAccepted()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var context = CreateAuthenticatedContext();

        context.Request.Headers["X-Tenant-Id"] =
            tenantId.ToString();
        context.Request.Headers["X-Branch-Id"] =
            branchId.ToString();

        var tenantContext = new TenantContext();

        await InvokeAsync(
            context,
            tenantContext,
            [Guid.NewGuid(), tenantId],
            [Guid.NewGuid(), branchId]);

        Assert.Equal(tenantId, tenantContext.ActiveTenantId);
        Assert.Equal(branchId, tenantContext.ActiveBranchId);
    }

    private static Task InvokeAsync(
        DefaultHttpContext httpContext,
        TenantContext tenantContext,
        Guid[] tenantIds,
        Guid[] branchIds)
    {
        var middleware =
            new TenantResolutionMiddleware(_ => Task.CompletedTask);

        return middleware.InvokeAsync(
            httpContext,
            tenantContext,
            new StubTenantResolver(tenantIds),
            new StubBranchResolver(branchIds));
    }

    private static DefaultHttpContext CreateAuthenticatedContext()
    {
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
            [new Claim(
                OpenIddictConstants.Claims.Subject,
                userId.ToString())],
            "test");

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
    }

    private sealed class StubTenantResolver(Guid[] tenantIds)
        : ITenantAccessResolver
    {
        public Task<(Guid[] Readable, Guid[] Writable)> ResolveAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult((tenantIds, tenantIds));
    }

    private sealed class StubBranchResolver(Guid[] branchIds)
        : IBranchAccessResolver
    {
        public Task<(Guid[] Readable, Guid[] Writable)> ResolveAsync(
            Guid userId,
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult((branchIds, branchIds));
    }
}
