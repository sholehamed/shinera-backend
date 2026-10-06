using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;

namespace Application.Tests.Authorization;

public sealed class PermissionAuthorizationServiceTests
{
    [Fact]
    public async Task TenantScopedRolePermission_Allows()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(tenantId, userId);
        var permissionId = await fixture.SeedPermissionAsync(
            "appointments",
            "view");

        var role = new Role(tenantId, "Owner", null);
        fixture.Db.Roles.Add(role);
        fixture.Db.UserRoles.Add(new UserRole(tenantId, userId, role.Id));
        fixture.Db.PermissionAssignments.Add(
            new PermissionAssignment(
                tenantId,
                permissionId,
                PermissionSubjectType.Role,
                role.Id,
                PermissionScopeType.Tenant));

        await fixture.Db.SaveChangesAsync();

        var decision = await fixture.Service.AuthorizeAsync(
            userId,
            "appointments",
            "view",
            new PermissionScopeContext());

        Assert.True(decision.IsAllowed);
        Assert.Equal(PermissionScopeType.Tenant, decision.MatchedScope);
    }

    [Fact]
    public async Task MissingPermission_DefaultDenies()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(tenantId, userId);
        await fixture.SeedPermissionAsync("appointments", "create");

        var decision = await fixture.Service.HasPermissionAsync(
            userId,
            "appointments",
            "create");

        Assert.False(decision.IsAllowed);
        Assert.Equal(
            PermissionDecisionCode.PermissionRequired,
            decision.Code);
    }

    [Fact]
    public async Task WrongTenant_DeniesBeforePermissionEvaluation()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(
            tenantA,
            userId,
            createMembership: false);

        using (fixture.TenantContext.DisableFilter())
        {
            fixture.Db.Tenants.Add(new Tenant(
                tenantB,
                null,
                "Tenant B",
                $"tenant-{tenantB:N}",
                "tenant-b.example.test"));

            fixture.Db.TenantMemberships.Add(
                new TenantMembership(tenantB, userId));

            await fixture.Db.SaveChangesAsync();
        }

        var decision = await fixture.Service.HasPermissionAsync(
            userId,
            "appointments",
            "view");

        Assert.False(decision.IsAllowed);
        Assert.Equal(
            PermissionDecisionCode.TenantMembershipRequired,
            decision.Code);
    }

    [Fact]
    public async Task FilterBypass_DoesNotPermitCrossTenantGrant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(
            tenantA,
            userId,
            createMembership: false);

        Guid permissionId;

        using (fixture.TenantContext.DisableFilter())
        {
            fixture.Db.Tenants.Add(new Tenant(
                tenantB,
                null,
                "Tenant B",
                $"tenant-{tenantB:N}",
                "tenant-b.example.test"));

            fixture.Db.TenantMemberships.Add(
                new TenantMembership(tenantB, userId));

            permissionId = await fixture.SeedPermissionAsync(
                "appointments",
                "view");

            fixture.Db.PermissionAssignments.Add(
                new PermissionAssignment(
                    tenantB,
                    permissionId,
                    PermissionSubjectType.User,
                    userId,
                    PermissionScopeType.Tenant));

            await fixture.Db.SaveChangesAsync();

            var decision = await fixture.Service.HasPermissionAsync(
                userId,
                "appointments",
                "view");

            Assert.False(decision.IsAllowed);
            Assert.Equal(
                PermissionDecisionCode.TenantMembershipRequired,
                decision.Code);
        }
    }

    [Fact]
    public async Task EffectivePermissions_ReturnKeyAndAssignmentScope()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(tenantId, userId);
        var permissionId = await fixture.SeedPermissionAsync(
            "customers",
            "view");

        fixture.Db.PermissionAssignments.Add(
            new PermissionAssignment(
                tenantId,
                permissionId,
                PermissionSubjectType.User,
                userId,
                PermissionScopeType.Own));

        await fixture.Db.SaveChangesAsync();

        var permissions =
            await fixture.Service.GetEffectivePermissionsAsync(userId);

        var permission = Assert.Single(permissions);

        Assert.Equal("customers.view", permission.Key);
        Assert.Equal("Own", permission.Scope);
        Assert.Null(permission.ScopeReferenceId);
    }

    [Fact]
    public async Task InactiveUser_DeniesExistingPermissionGrant()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(tenantId, userId);
        var permissionId = await fixture.SeedPermissionAsync(
            "customers",
            "view");

        fixture.Db.PermissionAssignments.Add(
            new PermissionAssignment(
                tenantId,
                permissionId,
                PermissionSubjectType.User,
                userId,
                PermissionScopeType.Tenant));

        await fixture.Db.SaveChangesAsync();

        var user = await fixture.Db.Users
            .SingleAsync(x => x.Id == userId);

        user.IsActive = false;
        await fixture.Db.SaveChangesAsync();

        var decision = await fixture.Service.HasPermissionAsync(
            userId,
            "customers",
            "view");

        Assert.False(decision.IsAllowed);
        Assert.Equal(
            PermissionDecisionCode.UserInactive,
            decision.Code);
    }

    [Fact]
    public async Task InactiveTenant_DeniesExistingPermissionGrant()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(tenantId, userId);
        var permissionId = await fixture.SeedPermissionAsync(
            "customers",
            "view");

        fixture.Db.PermissionAssignments.Add(
            new PermissionAssignment(
                tenantId,
                permissionId,
                PermissionSubjectType.User,
                userId,
                PermissionScopeType.Tenant));

        await fixture.Db.SaveChangesAsync();

        var tenant = await fixture.Db.Tenants
            .SingleAsync(x => x.Id == tenantId);

        tenant.IsActive = false;
        await fixture.Db.SaveChangesAsync();

        var decision = await fixture.Service.HasPermissionAsync(
            userId,
            "customers",
            "view");

        Assert.False(decision.IsAllowed);
        Assert.Equal(
            PermissionDecisionCode.TenantMembershipRequired,
            decision.Code);
    }

    [Fact]
    public async Task EndpointPermissionGate_AllowsOwnGrantWithoutResourceContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(tenantId, userId);
        var permissionId = await fixture.SeedPermissionAsync(
            "appointments",
            "view");

        fixture.Db.PermissionAssignments.Add(
            new PermissionAssignment(
                tenantId,
                permissionId,
                PermissionSubjectType.User,
                userId,
                PermissionScopeType.Own));

        await fixture.Db.SaveChangesAsync();

        var gate = await fixture.Service.HasPermissionAsync(
            userId,
            "appointments",
            "view");

        var resourceCheck = await fixture.Service.AuthorizeAsync(
            userId,
            "appointments",
            "view",
            new PermissionScopeContext(
                OwnerUserId: Guid.NewGuid()));

        Assert.True(gate.IsAllowed);
        Assert.Equal(PermissionScopeType.Own, gate.MatchedScope);

        Assert.False(resourceCheck.IsAllowed);
        Assert.Equal(
            PermissionDecisionCode.ScopeDenied,
            resourceCheck.Code);
    }

    [Fact]
    public async Task OwnScope_AllowsOnlyOwnedResource()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(tenantId, userId);
        var permissionId = await fixture.SeedPermissionAsync(
            "appointments",
            "view");

        fixture.Db.PermissionAssignments.Add(
            new PermissionAssignment(
                tenantId,
                permissionId,
                PermissionSubjectType.User,
                userId,
                PermissionScopeType.Own));

        await fixture.Db.SaveChangesAsync();

        var own = await fixture.Service.AuthorizeAsync(
            userId,
            "appointments",
            "view",
            new PermissionScopeContext(OwnerUserId: userId));

        var other = await fixture.Service.AuthorizeAsync(
            userId,
            "appointments",
            "view",
            new PermissionScopeContext(OwnerUserId: Guid.NewGuid()));

        Assert.True(own.IsAllowed);
        Assert.Equal(PermissionScopeType.Own, own.MatchedScope);

        Assert.False(other.IsAllowed);
        Assert.Equal(PermissionDecisionCode.ScopeDenied, other.Code);
    }

    [Fact]
    public async Task BranchScope_RequiresValidatedBranchAccess()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(tenantId, userId);
        var permissionId = await fixture.SeedPermissionAsync(
            "appointments",
            "view");

        fixture.Db.PermissionAssignments.Add(
            new PermissionAssignment(
                tenantId,
                permissionId,
                PermissionSubjectType.User,
                userId,
                PermissionScopeType.Branch,
                branchId));

        await fixture.Db.SaveChangesAsync();

        var validated = await fixture.Service.AuthorizeAsync(
            userId,
            "appointments",
            "view",
            new PermissionScopeContext(
                BranchId: branchId,
                BranchAccessValidated: true));

        var unvalidated = await fixture.Service.AuthorizeAsync(
            userId,
            "appointments",
            "view",
            new PermissionScopeContext(
                BranchId: branchId,
                BranchAccessValidated: false));

        var wrongBranch = await fixture.Service.AuthorizeAsync(
            userId,
            "appointments",
            "view",
            new PermissionScopeContext(
                BranchId: Guid.NewGuid(),
                BranchAccessValidated: true));

        Assert.True(validated.IsAllowed);
        Assert.False(unvalidated.IsAllowed);
        Assert.False(wrongBranch.IsAllowed);
    }

    [Fact]
    public async Task ChildScope_IsReservedAndDenied()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(tenantId, userId);
        var permissionId = await fixture.SeedPermissionAsync(
            "staff",
            "manage");

        fixture.Db.PermissionAssignments.Add(
            new PermissionAssignment(
                tenantId,
                permissionId,
                PermissionSubjectType.User,
                userId,
                PermissionScopeType.Child));

        await fixture.Db.SaveChangesAsync();

        var decision = await fixture.Service.AuthorizeAsync(
            userId,
            "staff",
            "manage",
            new PermissionScopeContext());

        Assert.False(decision.IsAllowed);
        Assert.Equal(PermissionDecisionCode.ScopeDenied, decision.Code);
    }

    [Fact]
    public async Task LegacyExplicitDeny_DoesNotOverrideNewAllowAssignment()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var fixture = await CreateFixtureAsync(tenantId, userId);
        var permissionId = await fixture.SeedPermissionAsync(
            "customers",
            "view");

        fixture.Db.UserPermissions.Add(new UserPermission
        {
            TenantId = tenantId,
            UserId = userId,
            PermissionId = permissionId,
            IsGranted = false
        });

        fixture.Db.PermissionAssignments.Add(
            new PermissionAssignment(
                tenantId,
                permissionId,
                PermissionSubjectType.User,
                userId,
                PermissionScopeType.Tenant));

        await fixture.Db.SaveChangesAsync();

        var decision = await fixture.Service.HasPermissionAsync(
            userId,
            "customers",
            "view");

        Assert.True(decision.IsAllowed);
    }

    private static async Task<Fixture> CreateFixtureAsync(
        Guid tenantId,
        Guid userId,
        bool createMembership = true)
    {
        var tenantContext = new TenantContext();
        tenantContext.Initialize(
            userId,
            false,
            [tenantId],
            [tenantId],
            tenantId);

        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new AuthorizationTestDbContext(
            options,
            tenantContext);

        await db.Database.EnsureCreatedAsync();

        using (tenantContext.DisableFilter())
        {
            db.Tenants.Add(new Tenant(
                tenantId,
                null,
                "Tenant",
                $"tenant-{tenantId:N}",
                "example.test"));

            var user = new User(
                userId,
                $"user-{userId:N}",
                $"{userId:N}@example.test",
                "Test",
                "User")
            {
                PasswordHash = "test-password-hash",
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            };

            db.Users.Add(user);

            if (createMembership)
            {
                db.TenantMemberships.Add(
                    new TenantMembership(tenantId, userId));
            }

            await db.SaveChangesAsync();
        }

        return new Fixture(
            connection,
            db,
            tenantContext,
            new PermissionAuthorizationService(db, tenantContext));
    }

    private sealed class AuthorizationTestDbContext(
        DbContextOptions<IdentityDbContext> options,
        TenantContext tenantContext)
        : IdentityDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var property = entityType.FindProperty("RowVersion");
                if (property is null)
                    continue;

                property.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
            }
        }
    }

    private sealed class Fixture(
        SqliteConnection connection,
        IdentityDbContext db,
        TenantContext tenantContext,
        PermissionAuthorizationService service)
        : IAsyncDisposable
    {
        public IdentityDbContext Db { get; } = db;
        public TenantContext TenantContext { get; } = tenantContext;
        public PermissionAuthorizationService Service { get; } = service;

        public async Task<Guid> SeedPermissionAsync(
            string resourceCode,
            string action)
        {
            var module = new Module
            {
                Code = $"module-{Guid.NewGuid():N}",
                Title = "Module",
                Description = "Test",
                SortOrder = 0,
                IsActive = true
            };

            var resource = new Resource(
                module.Id,
                resourceCode,
                resourceCode,
                null,
                0);

            module.Resources = [resource];

            var permission = new Permission(
                $"{resourceCode}.{action}",
                resource,
                action,
                null);

            Db.Modules.Add(module);
            Db.Permissions.Add(permission);
            await Db.SaveChangesAsync();

            return permission.Id;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
