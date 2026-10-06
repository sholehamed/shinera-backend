using Application.SharedKernel.Models;
using Application.SharedKernel.Registration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Modules.System.Subscription.Application.Registration;
using Modules.System.Subscription.Domain.Entities;
using Modules.System.Subscription.Infrastructure.Persistence.Contexts;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Application.Features.BusinessProfiles;
using Modules.System.Identity.Application.Features.Registration.Commands;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;
using System.Data.Common;

namespace Application.Tests.Registration;

public sealed class RegistrationCommandTests
{
    [Fact]
    public async Task Register_CreatesCompleteWorkspaceGraph()
    {
        await using var fixture = await CreateFixtureAsync();

        var provisioner = new StubSubscriptionProvisioner(
            Result<RegistrationSubscriptionReceipt>.Success(
                new RegistrationSubscriptionReceipt(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "salon-pro")));

        var handler = new RegisterWorkspaceCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            new PasswordHasher<User>(),
            provisioner,
            TimeProvider.System);

        var result = await handler.Handle(
            ValidCommand(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("salon-pro", result.Value.PlanKey);
        Assert.Equal("salon-pro", provisioner.PlanKey);

        using (fixture.TenantContext.DisableFilter())
        {
            var tenant = await fixture.Db.Tenants
                .AsNoTracking()
                .SingleAsync(x => x.Id == result.Value.TenantId);

            var user = await fixture.Db.Users
                .AsNoTracking()
                .SingleAsync(x => x.Id == result.Value.UserId);

            var profile = await fixture.Db.BusinessProfiles
                .AsNoTracking()
                .SingleAsync(x =>
                    x.TenantId == result.Value.TenantId);

            var branch = await fixture.Db.Branches
                .AsNoTracking()
                .SingleAsync(x =>
                    x.Id == result.Value.BranchId);

            var tenantMembership =
                await fixture.Db.TenantMemberships
                    .AsNoTracking()
                    .SingleAsync(x =>
                        x.TenantId == tenant.Id &&
                        x.UserId == user.Id);

            var branchMembership =
                await fixture.Db.BranchMemberships
                    .AsNoTracking()
                    .SingleAsync(x =>
                        x.TenantId == tenant.Id &&
                        x.BranchId == branch.Id &&
                        x.UserId == user.Id);

            var ownerRole = await fixture.Db.Roles
                .AsNoTracking()
                .SingleAsync(x =>
                    x.TenantId == tenant.Id &&
                    x.NormalizedName == "OWNER");

            var userRole = await fixture.Db.UserRoles
                .AsNoTracking()
                .SingleAsync(x =>
                    x.TenantId == tenant.Id &&
                    x.UserId == user.Id &&
                    x.RoleId == ownerRole.Id);

            var ownerPermissions =
                await fixture.Db.PermissionAssignments
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenant.Id &&
                        x.SubjectType ==
                            PermissionSubjectType.Role &&
                        x.SubjectId == ownerRole.Id)
                    .ToListAsync();

            Assert.Equal("Demo Salon", tenant.Name);
            Assert.Equal("09120000000", user.Phone);
            Assert.Equal(BusinessMode.Salon, profile.Mode);
            Assert.Equal("Beauty Salon", profile.BusinessType);
            Assert.True(branch.IsMain);
            Assert.True(branch.IsActive);
            Assert.True(tenantMembership.IsActive);
            Assert.True(branchMembership.IsActive);
            Assert.NotNull(userRole);

            Assert.Equal(
                SystemPermissionCatalog
                    .WorkspaceOwnerPermissionKeys
                    .Length,
                ownerPermissions.Count);

            Assert.All(
                ownerPermissions,
                permission =>
                    Assert.Equal(
                        PermissionScopeType.Tenant,
                        permission.ScopeType));

            var verification =
                new PasswordHasher<User>()
                    .VerifyHashedPassword(
                        user,
                        user.PasswordHash,
                        "StrongPass123!");

            Assert.NotEqual(
                PasswordVerificationResult.Failed,
                verification);
        }
    }

    [Fact]
    public async Task Register_WhenOwnerEmailAlreadyExists_ReturnsConflict()
    {
        await using var fixture = await CreateFixtureAsync();

        using (fixture.TenantContext.DisableFilter())
        {
            var existing = new User(
                "existing",
                "owner@example.com",
                "Existing",
                "Owner")
            {
                PasswordHash = "hash"
            };

            fixture.Db.Users.Add(existing);
            await fixture.Db.SaveChangesAsync();
        }

        var provisioner = new StubSubscriptionProvisioner(
            Result<RegistrationSubscriptionReceipt>.Success(
                new RegistrationSubscriptionReceipt(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "salon-pro")));

        var handler = new RegisterWorkspaceCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            new PasswordHasher<User>(),
            provisioner,
            TimeProvider.System);

        var result = await handler.Handle(
            ValidCommand(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "registration.owner_email_exists",
            result.Error.Code);
        Assert.False(provisioner.WasCalled);
    }

    [Fact]
    public async Task Register_WhenSubscriptionProvisioningFails_RollsBackIdentityGraph()
    {
        await using var fixture = await CreateFixtureAsync();

        var provisioner = new StubSubscriptionProvisioner(
            Result<RegistrationSubscriptionReceipt>.Failure(
                Error.Validation(
                    "registration.plan_invalid",
                    "Invalid plan.")));

        var handler = new RegisterWorkspaceCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            new PasswordHasher<User>(),
            provisioner,
            TimeProvider.System);

        var result = await handler.Handle(
            ValidCommand(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "registration.plan_invalid",
            result.Error.Code);

        await using var verificationDb =
            fixture.CreateFreshDbContext();

        using (fixture.TenantContext.DisableFilter())
        {
            Assert.Equal(
                0,
                await verificationDb.Tenants.CountAsync());

            Assert.Equal(
                0,
                await verificationDb.BusinessProfiles.CountAsync());

            Assert.Equal(
                0,
                await verificationDb.Branches.CountAsync());

            Assert.Equal(
                0,
                await verificationDb.TenantMemberships.CountAsync());

            Assert.Equal(
                0,
                await verificationDb.BranchMemberships.CountAsync());

            Assert.Equal(
                0,
                await verificationDb.Roles.CountAsync());

            Assert.Equal(
                0,
                await verificationDb.UserRoles.CountAsync());

            Assert.Equal(
                0,
                await verificationDb.PermissionAssignments.CountAsync());

            // Permission catalog users are not seeded; only the attempted
            // registration user would exist in this test database.
            Assert.Equal(
                0,
                await verificationDb.Users.CountAsync());
        }
    }

    [Fact]
    public async Task SubscriptionProvisioner_JoinsIdentityTransaction_AndRollsBackAtomically()
    {
        var tenantContext = new TenantContext();

        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var identityOptions =
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlite(connection)
                .Options;

        var subscriptionOptions =
            new DbContextOptionsBuilder<SubscriptionDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var identityDb =
            new RegistrationTestDbContext(
                identityOptions,
                tenantContext);

        await identityDb.Database.EnsureCreatedAsync();

        await using var subscriptionDb =
            new RegistrationSubscriptionTestDbContext(
                subscriptionOptions,
                tenantContext);

        var creator = subscriptionDb.Database
            .GetService<IRelationalDatabaseCreator>();

        await creator.CreateTablesAsync();

        var plan = new Plan(
            "salon-pro",
            "Salon Pro");

        subscriptionDb.Plans.Add(plan);
        await subscriptionDb.SaveChangesAsync();

        var tenantId = Guid.NewGuid();

        using (tenantContext.DisableFilter())
        {
            await using var transaction =
                await identityDb.Database
                    .BeginTransactionAsync();

            identityDb.Tenants.Add(
                new Tenant(tenantId)
                {
                    Name = "Atomic Tenant",
                    Slug = $"tenant-{tenantId:N}"
                });

            await identityDb.SaveChangesAsync();

            var provisioner =
                new RegistrationSubscriptionProvisioner(
                    subscriptionDb);

            var provisionResult =
                await provisioner.ProvisionAsync(
                    tenantId,
                    "salon-pro",
                    DateTimeOffset.UtcNow,
                    transaction.GetDbTransaction());

            Assert.True(provisionResult.IsSuccess);

            await transaction.RollbackAsync();
        }

        identityDb.ChangeTracker.Clear();
        subscriptionDb.ChangeTracker.Clear();

        using (tenantContext.DisableFilter())
        {
            Assert.False(
                await identityDb.Tenants
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == tenantId));

            Assert.False(
                await subscriptionDb.Subscriptions
                    .IgnoreQueryFilters(["tenant"])
                    .AsNoTracking()
                    .AnyAsync(x => x.TenantId == tenantId));
        }
    }

    [Fact]
    public async Task BusinessProfile_Update_ChangesCurrentTenantProfile()
    {
        await using var fixture = await CreateFixtureAsync();

        var tenantId = Guid.NewGuid();

        fixture.TenantContext.Initialize(
            Guid.NewGuid(),
            false,
            [tenantId],
            [tenantId],
            tenantId);

        using (fixture.TenantContext.DisableFilter())
        {
            fixture.Db.Tenants.Add(
                new Tenant(tenantId)
                {
                    Name = "Tenant",
                    Slug = $"tenant-{tenantId:N}"
                });

            fixture.Db.BusinessProfiles.Add(
                new BusinessProfile(
                    tenantId,
                    "Old",
                    "Old Type",
                    BusinessMode.Solo));

            await fixture.Db.SaveChangesAsync();
        }

        var handler =
            new UpdateBusinessProfileCommandHandler(
                fixture.Db);

        await handler.Handle(
            new UpdateBusinessProfileCommand(
                "New",
                "New Type",
                BusinessMode.Salon,
                "021",
                "business@example.com",
                "Tehran",
                "Address",
                "Description"),
            CancellationToken.None);

        var profile = await fixture.Db.BusinessProfiles
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal("New", profile.DisplayName);
        Assert.Equal("New Type", profile.BusinessType);
        Assert.Equal(BusinessMode.Salon, profile.Mode);
        Assert.Equal("Tehran", profile.City);
    }

    private static RegisterWorkspaceCommand ValidCommand() =>
        new(
            "salon-pro",
            new RegistrationBusiness(
                "Demo Salon",
                "Beauty Salon",
                BusinessMode.Salon,
                "02100000000",
                "business@example.com",
                "Tehran",
                "Business address"),
            new RegistrationOwner(
                "Demo",
                "Owner",
                "09120000000",
                "owner@example.com",
                "StrongPass123!"),
            new RegistrationBranch(
                "Main Branch",
                "02100000000",
                "Branch address"));

    private static async Task<Fixture> CreateFixtureAsync()
    {
        var tenantContext = new TenantContext();

        var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlite(connection)
                .Options;

        var db = new RegistrationTestDbContext(
            options,
            tenantContext);

        await db.Database.EnsureCreatedAsync();

        await SeedWorkspacePermissionCatalogAsync(
            db,
            tenantContext);

        return new Fixture(
            connection,
            options,
            db,
            tenantContext);
    }

    private static async Task SeedWorkspacePermissionCatalogAsync(
        IdentityDbContext db,
        TenantContext tenantContext)
    {
        using (tenantContext.DisableFilter())
        {
            var module =
                new Modules.System.Identity.Domain.Entities.Module(
                    "system",
                    "System",
                    null,
                    0);

            db.Modules.Add(module);

            foreach (var resourceGroup in
                     SystemPermissionCatalog
                         .WorkspaceOwnerPermissionKeys
                         .GroupBy(
                             key => key.Split('.')[0]))
            {
                var resource = new Resource(
                    module.Id,
                    resourceGroup.Key,
                    resourceGroup.Key,
                    null,
                    0);

                db.Resources.Add(resource);

                foreach (var key in resourceGroup)
                {
                    db.Permissions.Add(
                        new Permission(
                            key,
                            resource,
                            key,
                            null));
                }
            }

            await db.SaveChangesAsync();
        }
    }

    private sealed class StubSubscriptionProvisioner(
        Result<RegistrationSubscriptionReceipt> result)
        : IRegistrationSubscriptionProvisioner
    {
        public bool WasCalled { get; private set; }
        public string? PlanKey { get; private set; }

        public Task<Result<RegistrationSubscriptionReceipt>>
            ProvisionAsync(
                Guid tenantId,
                string planKey,
                DateTimeOffset startedAtUtc,
                DbTransaction transaction,
                CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            PlanKey = planKey;
            return Task.FromResult(result);
        }
    }

    private sealed class RegistrationTestDbContext(
        DbContextOptions<IdentityDbContext> options,
        TenantContext tenantContext)
        : IdentityDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var entityType in
                     builder.Model.GetEntityTypes())
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

    private sealed class RegistrationSubscriptionTestDbContext(
        DbContextOptions<SubscriptionDbContext> options,
        TenantContext tenantContext)
        : SubscriptionDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var entityType in
                     builder.Model.GetEntityTypes())
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

    private sealed class Fixture(
        SqliteConnection connection,
        DbContextOptions<IdentityDbContext> options,
        IdentityDbContext db,
        TenantContext tenantContext)
        : IAsyncDisposable
    {
        public SqliteConnection Connection { get; } =
            connection;

        public DbContextOptions<IdentityDbContext> Options { get; } =
            options;

        public IdentityDbContext Db { get; } = db;

        public TenantContext TenantContext { get; } =
            tenantContext;

        public IdentityDbContext CreateFreshDbContext() =>
            new RegistrationTestDbContext(
                Options,
                TenantContext);

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
