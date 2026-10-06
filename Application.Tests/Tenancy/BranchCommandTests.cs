using Application.SharedKernel.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Application.Features.Branches.Commands;
using Modules.System.Identity.Application.Features.Branches.Queries;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;

namespace Application.Tests.Tenancy;

public sealed class BranchCommandTests
{
    [Fact]
    public async Task Create_AddsExplicitMembershipForCreator()
    {
        await using var fixture =
            await CreateFixtureAsync();

        var handler =
            new BranchCreateCommandHandler(
                fixture.Db,
                fixture.TenantContext,
                fixture.TenantAuthorization);

        var branchId = await handler.Handle(
            new BranchCreateCommand(
                "North",
                "021-0000",
                "North address"),
            CancellationToken.None);

        var branch = await fixture.Db.Branches
            .AsNoTracking()
            .SingleAsync(x => x.Id == branchId);

        var membership =
            await fixture.Db.BranchMemberships
                .AsNoTracking()
                .SingleAsync(x =>
                    x.BranchId == branchId &&
                    x.UserId == fixture.UserId);

        Assert.Equal(
            fixture.TenantId,
            branch.TenantId);
        Assert.False(branch.IsMain);
        Assert.True(branch.IsActive);
        Assert.True(membership.IsActive);
    }

    [Fact]
    public async Task Create_WithOnlyBranchScopedPermission_IsDenied()
    {
        await using var fixture =
            await CreateFixtureAsync();

        var handler =
            new BranchCreateCommandHandler(
                fixture.Db,
                fixture.TenantContext,
                new StubAuthorizationService(
                    PermissionScopeType.Branch,
                    Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(
                new BranchCreateCommand(
                    "Denied",
                    null,
                    null),
                CancellationToken.None));
    }

    [Fact]
    public async Task List_WithBranchScopedPermission_ReturnsOnlyAccessibleGrantedBranch()
    {
        await using var fixture =
            await CreateFixtureAsync();

        var branchA = new Branch(
            fixture.TenantId,
            "A",
            isMain: true);

        var branchB = new Branch(
            fixture.TenantId,
            "B");

        fixture.Db.Branches.AddRange(
            branchA,
            branchB);

        await fixture.Db.SaveChangesAsync();

        fixture.TenantContext.Initialize(
            fixture.UserId,
            false,
            [fixture.TenantId],
            [fixture.TenantId],
            fixture.TenantId,
            [branchA.Id],
            [branchA.Id],
            branchA.Id);

        var handler =
            new BranchListQueryHandler(
                fixture.Db,
                fixture.TenantContext,
                new StubAuthorizationService(
                    PermissionScopeType.Branch,
                    branchA.Id));

        var result = await handler.Handle(
            new BranchListQuery(),
            CancellationToken.None);

        var branch = Assert.Single(result);
        Assert.Equal(branchA.Id, branch.Id);
    }

    [Fact]
    public async Task Update_WithGrantForAnotherBranch_IsDenied()
    {
        await using var fixture =
            await CreateFixtureAsync();

        var branchA = new Branch(
            fixture.TenantId,
            "A",
            isMain: true);

        var branchB = new Branch(
            fixture.TenantId,
            "B");

        fixture.Db.Branches.AddRange(
            branchA,
            branchB);

        await fixture.Db.SaveChangesAsync();

        fixture.TenantContext.Initialize(
            fixture.UserId,
            false,
            [fixture.TenantId],
            [fixture.TenantId],
            fixture.TenantId,
            [branchA.Id],
            [branchA.Id],
            branchA.Id);

        var handler =
            new BranchUpdateCommandHandler(
                fixture.Db,
                fixture.TenantContext,
                new StubAuthorizationService(
                    PermissionScopeType.Branch,
                    branchA.Id));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(
                new BranchUpdateCommand(
                    branchB.Id,
                    "Changed",
                    null,
                    null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_ReadableButNotWritableBranch_IsDenied()
    {
        await using var fixture =
            await CreateFixtureAsync();

        var branch = new Branch(
            fixture.TenantId,
            "Read only",
            isMain: true);

        fixture.Db.Branches.Add(branch);
        await fixture.Db.SaveChangesAsync();

        fixture.TenantContext.Initialize(
            fixture.UserId,
            false,
            [fixture.TenantId],
            [fixture.TenantId],
            fixture.TenantId,
            [branch.Id],
            [],
            branch.Id);

        var handler =
            new BranchUpdateCommandHandler(
                fixture.Db,
                fixture.TenantContext,
                new StubAuthorizationService(
                    PermissionScopeType.Branch,
                    branch.Id));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(
                new BranchUpdateCommand(
                    branch.Id,
                    "Changed",
                    null,
                    null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Disable_MainBranch_ReturnsConflict()
    {
        await using var fixture =
            await CreateFixtureAsync();

        var main = new Branch(
            fixture.TenantId,
            "Main",
            isMain: true);

        fixture.Db.Branches.Add(main);
        await fixture.Db.SaveChangesAsync();

        var handler =
            new BranchDisableCommandHandler(
                fixture.Db,
                fixture.TenantContext,
                fixture.TenantAuthorization);

        var result = await handler.Handle(
            new BranchDisableCommand(main.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "branch.main_disable_forbidden",
            result.Error.Code);
        Assert.True(main.IsActive);
    }

    [Fact]
    public async Task SetMain_MovesMainFlagAtomically()
    {
        await using var fixture =
            await CreateFixtureAsync();

        var first = new Branch(
            fixture.TenantId,
            "First",
            isMain: true);

        var second = new Branch(
            fixture.TenantId,
            "Second");

        fixture.Db.Branches.AddRange(
            first,
            second);

        await fixture.Db.SaveChangesAsync();

        var handler =
            new SetMainBranchCommandHandler(
                fixture.Db,
                fixture.TenantContext,
                fixture.TenantAuthorization);

        var result = await handler.Handle(
            new SetMainBranchCommand(second.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var branches = await fixture.Db.Branches
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();

        Assert.Single(
            branches.Where(x => x.IsMain));
        Assert.False(
            branches.Single(x => x.Id == first.Id)
                .IsMain);
        Assert.True(
            branches.Single(x => x.Id == second.Id)
                .IsMain);
    }

    [Fact]
    public async Task SetMain_InactiveBranch_ReturnsConflict()
    {
        await using var fixture =
            await CreateFixtureAsync();

        var current = new Branch(
            fixture.TenantId,
            "Main",
            isMain: true);

        var inactive = new Branch(
            fixture.TenantId,
            "Inactive",
            isActive: false);

        fixture.Db.Branches.AddRange(
            current,
            inactive);

        await fixture.Db.SaveChangesAsync();

        var handler =
            new SetMainBranchCommandHandler(
                fixture.Db,
                fixture.TenantContext,
                fixture.TenantAuthorization);

        var result = await handler.Handle(
            new SetMainBranchCommand(inactive.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "branch.inactive_main_forbidden",
            result.Error.Code);
        Assert.True(current.IsMain);
        Assert.False(inactive.IsMain);
    }

    private static async Task<Fixture> CreateFixtureAsync()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var tenantContext = new TenantContext();
        tenantContext.Initialize(
            userId,
            false,
            [tenantId],
            [tenantId],
            tenantId);

        var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlite(connection)
                .Options;

        var db =
            new BranchCommandTestDbContext(
                options,
                tenantContext);

        await db.Database.EnsureCreatedAsync();

        using (tenantContext.DisableFilter())
        {
            db.Tenants.Add(
                new Tenant(
                    tenantId,
                    null,
                    "Tenant",
                    $"tenant-{tenantId:N}",
                    "tenant.example.test"));

            db.Users.Add(
                new User(
                    userId,
                    $"user-{userId:N}",
                    $"{userId:N}@example.test",
                    "Test",
                    "User")
                {
                    PasswordHash =
                        "test-password-hash",
                    SecurityStamp =
                        Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp =
                        Guid.NewGuid().ToString("N")
                });

            db.TenantMemberships.Add(
                new TenantMembership(
                    tenantId,
                    userId));

            await db.SaveChangesAsync();
        }

        return new Fixture(
            connection,
            db,
            tenantContext,
            tenantId,
            userId);
    }

    private sealed class BranchCommandTestDbContext(
        DbContextOptions<IdentityDbContext> options,
        TenantContext tenantContext)
        : IdentityDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var rowVersion =
                    entityType.FindProperty("RowVersion");

                if (rowVersion is not null)
                {
                    rowVersion.ValueGenerated =
                        ValueGenerated.Never;
                }
            }
        }
    }

    private sealed class StubAuthorizationService(
        PermissionScopeType scope,
        Guid? scopeReferenceId = null)
        : IPermissionAuthorizationService
    {
        public Task<PermissionDecision> HasPermissionAsync(
            Guid userId,
            string resource,
            string action,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                PermissionDecision.Allow(
                    $"{resource}.{action}",
                    scope));

        public Task<PermissionDecision> AuthorizeAsync(
            Guid userId,
            string resource,
            string action,
            PermissionScopeContext resourceContext,
            CancellationToken cancellationToken = default)
        {
            var allowed = scope switch
            {
                PermissionScopeType.Tenant => true,
                PermissionScopeType.Branch =>
                    resourceContext.BranchAccessValidated &&
                    resourceContext.BranchId.HasValue &&
                    (
                        !scopeReferenceId.HasValue ||
                        scopeReferenceId ==
                            resourceContext.BranchId
                    ),
                _ => false
            };

            return Task.FromResult(
                allowed
                    ? PermissionDecision.Allow(
                        $"{resource}.{action}",
                        scope)
                    : PermissionDecision.Deny(
                        $"{resource}.{action}",
                        PermissionDecisionCode.ScopeDenied));
        }

        public Task<IReadOnlyList<EffectivePermissionDto>>
            GetGrantedScopesAsync(
                Guid userId,
                string resource,
                string action,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EffectivePermissionDto>>(
            [
                new EffectivePermissionDto(
                    $"{resource}.{action}",
                    scope.ToString(),
                    scopeReferenceId)
            ]);

        public Task<IReadOnlyList<EffectivePermissionDto>>
            GetEffectivePermissionsAsync(
                Guid userId,
                CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class Fixture(
        SqliteConnection connection,
        IdentityDbContext db,
        TenantContext tenantContext,
        Guid tenantId,
        Guid userId)
        : IAsyncDisposable
    {
        public IdentityDbContext Db { get; } = db;
        public TenantContext TenantContext { get; } =
            tenantContext;
        public Guid TenantId { get; } = tenantId;
        public Guid UserId { get; } = userId;

        public IPermissionAuthorizationService
            TenantAuthorization { get; } =
            new StubAuthorizationService(
                PermissionScopeType.Tenant);

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
