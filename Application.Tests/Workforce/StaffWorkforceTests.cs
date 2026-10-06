using Application.SharedKernel.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;
using Modules.System.Services.Domain.Entities;
using Modules.System.Services.Infrastructure.Persistence.Contexts;
using Modules.System.Workforce.Application.Features.Staff;
using Modules.System.Workforce.Domain.Entities;
using Modules.System.Workforce.Infrastructure.Persistence.Contexts;
using Modules.System.Workforce.Infrastructure.Persistence.Interceptors;
using ServiceEntity = Modules.System.Services.Domain.Entities.Service;
using StaffEntity = Modules.System.Workforce.Domain.Entities.Staff;

namespace Application.Tests.Workforce;

public sealed class StaffWorkforceTests
{
    [Fact]
    public async Task CreateStaff_DoesNotRequireLoginUser()
    {
        await using var fixture = await CreateFixtureAsync();

        var handler = new CreateStaffCommandHandler(
            fixture.Workforce,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        var id = await handler.Handle(
            new CreateStaffCommand(
                "Sara",
                "Ahmadi",
                "09120000000",
                "sara@example.test"),
            CancellationToken.None);

        var staff = await fixture.Workforce.Staff
            .AsNoTracking()
            .SingleAsync(x => x.Id == id);

        Assert.Null(staff.UserId);
        Assert.Equal(fixture.TenantId, staff.TenantId);
        Assert.True(staff.IsActive);
    }

    [Fact]
    public async Task TenantFilter_HidesAnotherTenantsStaff()
    {
        await using var fixture = await CreateFixtureAsync();

        using (fixture.TenantContext.DisableFilter())
        {
            fixture.Workforce.Staff.AddRange(
                new StaffEntity(
                    fixture.TenantId,
                    "Visible",
                    "Staff",
                    "1",
                    "visible@example.test"),
                new StaffEntity(
                    Guid.NewGuid(),
                    "Hidden",
                    "Staff",
                    "2",
                    "hidden@example.test"));

            await fixture.Workforce.SaveChangesAsync();
        }

        var names = await fixture.Workforce.Staff
            .AsNoTracking()
            .Select(x => x.FirstName)
            .ToListAsync();

        var name = Assert.Single(names);
        Assert.Equal("Visible", name);
    }

    [Fact]
    public async Task AssignBranches_RejectsAnotherTenantBranch()
    {
        await using var fixture = await CreateFixtureAsync();

        var staff = await fixture.AddStaffAsync();
        var foreignBranch = await fixture.AddBranchAsync(
            Guid.NewGuid(),
            "Foreign");

        var handler = new ReplaceStaffBranchesCommandHandler(
            fixture.Workforce,
            fixture.Identity,
            fixture.TenantContext,
            fixture.TenantAuthorization,
            new Application.Tests.TestDoubles.AllowStaffBookingConcurrencyGuard());

        var result = await handler.Handle(
            new ReplaceStaffBranchesCommand(
                staff.Id,
                [foreignBranch.Id]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "staff.branch_invalid",
            result.Error.Code);
    }

    [Fact]
    public async Task AssignServices_ReplacesAssignmentsIdempotently()
    {
        await using var fixture = await CreateFixtureAsync();

        var staff = await fixture.AddStaffAsync();
        var first = await fixture.AddServiceAsync("Haircut");
        var second = await fixture.AddServiceAsync("Color");

        var handler = new ReplaceStaffServicesCommandHandler(
            fixture.Workforce,
            fixture.Services,
            fixture.TenantContext,
            fixture.TenantAuthorization,
            new Application.Tests.TestDoubles.AllowStaffBookingConcurrencyGuard());

        var firstResult = await handler.Handle(
            new ReplaceStaffServicesCommand(
                staff.Id,
                [first.Id, first.Id, second.Id]),
            CancellationToken.None);

        var secondResult = await handler.Handle(
            new ReplaceStaffServicesCommand(
                staff.Id,
                [second.Id]),
            CancellationToken.None);

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);

        var links = await fixture.Workforce.StaffServices
            .AsNoTracking()
            .Where(x => x.StaffId == staff.Id)
            .ToListAsync();

        var link = Assert.Single(links);
        Assert.Equal(second.Id, link.ServiceId);
    }

    [Fact]
    public async Task AssignServices_RejectsAnotherTenantService()
    {
        await using var fixture = await CreateFixtureAsync();

        var staff = await fixture.AddStaffAsync();
        var foreignService = await fixture.AddServiceAsync(
            "Foreign",
            Guid.NewGuid());

        var handler = new ReplaceStaffServicesCommandHandler(
            fixture.Workforce,
            fixture.Services,
            fixture.TenantContext,
            fixture.TenantAuthorization,
            new Application.Tests.TestDoubles.AllowStaffBookingConcurrencyGuard());

        var result = await handler.Handle(
            new ReplaceStaffServicesCommand(
                staff.Id,
                [foreignService.Id]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "staff.service_invalid",
            result.Error.Code);
    }

    [Fact]
    public async Task StaffList_FiltersBySearchBranchServiceAndStatus()
    {
        await using var fixture = await CreateFixtureAsync();

        var branch = await fixture.AddBranchAsync(
            fixture.TenantId,
            "Main");

        var service = await fixture.AddServiceAsync("Haircut");

        var match = await fixture.AddStaffAsync("Sara", true);
        var other = await fixture.AddStaffAsync("Neda", true);
        var inactive = await fixture.AddStaffAsync("Mina", false);

        fixture.Workforce.StaffBranches.Add(
            new StaffBranch(
                fixture.TenantId,
                match.Id,
                branch.Id));

        fixture.Workforce.StaffServices.Add(
            new StaffService(
                fixture.TenantId,
                match.Id,
                service.Id));

        fixture.Workforce.StaffBranches.Add(
            new StaffBranch(
                fixture.TenantId,
                inactive.Id,
                branch.Id));

        fixture.Workforce.StaffServices.Add(
            new StaffService(
                fixture.TenantId,
                inactive.Id,
                service.Id));

        await fixture.Workforce.SaveChangesAsync();

        var handler = new StaffListQueryHandler(
            fixture.Workforce,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        var result = await handler.Handle(
            new StaffListQuery
            {
                Search = "Sara",
                BranchId = branch.Id,
                ServiceId = service.Id,
                IsActive = true,
                PageNumber = 1,
                PageSize = 20
            },
            CancellationToken.None);

        var item = Assert.Single(result.Data);
        Assert.Equal(match.Id, item.Id);
        Assert.Equal(1, result.TotalCount);
        Assert.DoesNotContain(
            result.Data,
            x => x.Id == other.Id);
    }

    [Fact]
    public async Task StaffDetails_ReturnsAssignedBranchAndServiceNames()
    {
        await using var fixture = await CreateFixtureAsync();

        var branch = await fixture.AddBranchAsync(
            fixture.TenantId,
            "Downtown");

        var service = await fixture.AddServiceAsync("Haircut");
        var staff = await fixture.AddStaffAsync();

        fixture.Workforce.StaffBranches.Add(
            new StaffBranch(
                fixture.TenantId,
                staff.Id,
                branch.Id));

        fixture.Workforce.StaffServices.Add(
            new StaffService(
                fixture.TenantId,
                staff.Id,
                service.Id));

        await fixture.Workforce.SaveChangesAsync();

        var handler = new StaffDetailsQueryHandler(
            fixture.Workforce,
            fixture.Identity,
            fixture.Services,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        var result = await handler.Handle(
            new StaffDetailsQuery(staff.Id),
            CancellationToken.None);

        Assert.Equal("Downtown", Assert.Single(result.Branches).Name);
        Assert.Equal("Haircut", Assert.Single(result.Services).Name);
    }

    [Fact]
    public async Task CreateStaff_WithOnlyBranchScope_IsDenied()
    {
        await using var fixture = await CreateFixtureAsync();

        var handler = new CreateStaffCommandHandler(
            fixture.Workforce,
            fixture.TenantContext,
            new StubAuthorizationService(
                PermissionScopeType.Branch));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(
                new CreateStaffCommand(
                    "Denied",
                    "Staff",
                    "09120000000",
                    "denied@example.test"),
                CancellationToken.None));
    }

    [Fact]
    public async Task DatabaseConstraint_PreventsDuplicateLinkedUserWithinTenant()
    {
        await using var fixture = await CreateFixtureAsync();

        var userId = Guid.NewGuid();

        fixture.Workforce.Staff.AddRange(
            new StaffEntity(
                fixture.TenantId,
                "A",
                "Staff",
                "1",
                "a@example.test",
                userId: userId),
            new StaffEntity(
                fixture.TenantId,
                "B",
                "Staff",
                "2",
                "b@example.test",
                userId: userId));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => fixture.Workforce.SaveChangesAsync());
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

        var workforceConnection =
            new SqliteConnection("Data Source=:memory:");
        var identityConnection =
            new SqliteConnection("Data Source=:memory:");
        var servicesConnection =
            new SqliteConnection("Data Source=:memory:");

        await workforceConnection.OpenAsync();
        await identityConnection.OpenAsync();
        await servicesConnection.OpenAsync();

        var workforceOptions =
            new DbContextOptionsBuilder<WorkforceDbContext>()
                .UseSqlite(workforceConnection)
                .AddInterceptors(
                    new WorkforceTenantSaveChangesInterceptor(
                        tenantContext))
                .Options;

        var identityOptions =
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlite(identityConnection)
                .Options;

        var servicesOptions =
            new DbContextOptionsBuilder<ServicesDbContext>()
                .UseSqlite(servicesConnection)
                .Options;

        var workforce = new TestWorkforceDbContext(
            workforceOptions,
            tenantContext);

        var identity = new TestIdentityDbContext(
            identityOptions,
            tenantContext);

        var services = new TestServicesDbContext(
            servicesOptions,
            tenantContext);

        await workforce.Database.EnsureCreatedAsync();
        await identity.Database.EnsureCreatedAsync();
        await services.Database.EnsureCreatedAsync();

        using (tenantContext.DisableFilter())
        {
            identity.Tenants.Add(
                new Tenant(tenantId)
                {
                    Name = "Tenant",
                    Slug = $"tenant-{tenantId:N}",
                    IsActive = true
                });

            await identity.SaveChangesAsync();
        }

        return new Fixture(
            workforceConnection,
            identityConnection,
            servicesConnection,
            workforce,
            identity,
            services,
            tenantContext,
            tenantId);
    }

    private sealed class TestWorkforceDbContext(
        DbContextOptions<WorkforceDbContext> options,
        TenantContext tenantContext)
        : WorkforceDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            DisableRowVersions(builder);
        }
    }

    private sealed class TestIdentityDbContext(
        DbContextOptions<IdentityDbContext> options,
        TenantContext tenantContext)
        : IdentityDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            DisableRowVersions(builder);
        }
    }

    private sealed class TestServicesDbContext(
        DbContextOptions<ServicesDbContext> options,
        TenantContext tenantContext)
        : ServicesDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            DisableRowVersions(builder);
        }
    }

    private static void DisableRowVersions(ModelBuilder builder)
    {
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

    private sealed class StubAuthorizationService(
        PermissionScopeType scope)
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
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                PermissionDecision.Allow(
                    $"{resource}.{action}",
                    scope));

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
                    null)
            ]);

        public Task<IReadOnlyList<EffectivePermissionDto>>
            GetEffectivePermissionsAsync(
                Guid userId,
                CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class Fixture(
        SqliteConnection workforceConnection,
        SqliteConnection identityConnection,
        SqliteConnection servicesConnection,
        WorkforceDbContext workforce,
        IdentityDbContext identity,
        ServicesDbContext services,
        TenantContext tenantContext,
        Guid tenantId)
        : IAsyncDisposable
    {
        public WorkforceDbContext Workforce { get; } = workforce;
        public IdentityDbContext Identity { get; } = identity;
        public ServicesDbContext Services { get; } = services;
        public TenantContext TenantContext { get; } = tenantContext;
        public Guid TenantId { get; } = tenantId;

        public IPermissionAuthorizationService TenantAuthorization { get; } =
            new StubAuthorizationService(
                PermissionScopeType.Tenant);

        public async Task<StaffEntity> AddStaffAsync(
            string firstName = "Sara",
            bool isActive = true)
        {
            var staff = new StaffEntity(
                TenantId,
                firstName,
                "Staff",
                Guid.NewGuid().ToString("N")[..11],
                $"{Guid.NewGuid():N}@example.test",
                isActive);

            Workforce.Staff.Add(staff);
            await Workforce.SaveChangesAsync();
            return staff;
        }

        public async Task<Branch> AddBranchAsync(
            Guid tenantId,
            string name)
        {
            var branch = new Branch(
                tenantId,
                name);

            using (TenantContext.DisableFilter())
            {
                if (tenantId != TenantId &&
                    !await Identity.Tenants.AnyAsync(
                        x => x.Id == tenantId))
                {
                    Identity.Tenants.Add(
                        new Tenant(tenantId)
                        {
                            Name = "Foreign",
                            Slug = $"tenant-{tenantId:N}",
                            IsActive = true
                        });
                }

                Identity.Branches.Add(branch);
                await Identity.SaveChangesAsync();
            }

            return branch;
        }

        public async Task<ServiceEntity> AddServiceAsync(
            string name,
            Guid? tenantId = null)
        {
            var ownerTenantId = tenantId ?? TenantId;

            var category = new ServiceCategory(
                ownerTenantId,
                $"{name} Category");

            var service = new ServiceEntity(
                ownerTenantId,
                category.Id,
                name,
                30,
                100_000m);

            if (ownerTenantId == TenantId)
            {
                Services.ServiceCategories.Add(category);
                Services.Services.Add(service);
                await Services.SaveChangesAsync();
                return service;
            }

            using (TenantContext.DisableFilter())
            {
                Services.ServiceCategories.Add(category);
                Services.Services.Add(service);
                await Services.SaveChangesAsync();
            }

            return service;
        }

        public async ValueTask DisposeAsync()
        {
            await Workforce.DisposeAsync();
            await Identity.DisposeAsync();
            await Services.DisposeAsync();

            await workforceConnection.DisposeAsync();
            await identityConnection.DisposeAsync();
            await servicesConnection.DisposeAsync();
        }
    }
}
