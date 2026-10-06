using Application.SharedKernel.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Services.Application.Features.ServiceCategories;
using Modules.System.Services.Application.Features.Services;
using Modules.System.Services.Domain.Entities;
using Modules.System.Services.Infrastructure.Persistence.Contexts;
using Modules.System.Services.Infrastructure.Persistence.Interceptors;
using ServiceEntity = Modules.System.Services.Domain.Entities.Service;

namespace Application.Tests.Services;

public sealed class ServiceCatalogTests
{
    [Fact]
    public async Task TenantFilter_HidesAnotherTenantsCatalog()
    {
        await using var fixture = await CreateFixtureAsync();

        var otherTenantId = Guid.NewGuid();

        using (fixture.TenantContext.DisableFilter())
        {
            fixture.Db.ServiceCategories.AddRange(
                new ServiceCategory(
                    fixture.TenantId,
                    "Visible"),
                new ServiceCategory(
                    otherTenantId,
                    "Hidden"));

            await fixture.Db.SaveChangesAsync();
        }

        var categories = await fixture.Db.ServiceCategories
            .AsNoTracking()
            .Select(x => x.Name)
            .ToListAsync();

        Assert.Single(categories);
        Assert.Equal("Visible", categories[0]);
    }

    [Fact]
    public async Task AddedCategory_IsStampedWithActiveTenant()
    {
        await using var fixture = await CreateFixtureAsync();

        var category = new ServiceCategory(
            Guid.Empty,
            "Hair");

        fixture.Db.ServiceCategories.Add(category);
        await fixture.Db.SaveChangesAsync();

        Assert.Equal(
            fixture.TenantId,
            category.TenantId);
    }

    [Fact]
    public async Task DatabaseConstraint_RejectsCrossTenantCategoryReference()
    {
        await using var fixture = await CreateFixtureAsync();

        var otherTenantId = Guid.NewGuid();
        var foreignCategory = new ServiceCategory(
            otherTenantId,
            "Foreign");

        using (fixture.TenantContext.DisableFilter())
        {
            fixture.Db.ServiceCategories.Add(
                foreignCategory);

            await fixture.Db.SaveChangesAsync();
        }

        var service = new ServiceEntity(
            fixture.TenantId,
            foreignCategory.Id,
            "Invalid",
            30,
            100_000m);

        fixture.Db.Services.Add(service);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => fixture.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task CreateService_InvalidCategory_ReturnsStableValidationError()
    {
        await using var fixture = await CreateFixtureAsync();

        var handler = new CreateServiceCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        var result = await handler.Handle(
            new CreateServiceCommand(
                Guid.NewGuid(),
                "Haircut",
                null,
                45,
                1_000_000m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "service.category_invalid",
            result.Error.Code);
    }

    [Fact]
    public async Task CreateService_WithBranchScope_IsDenied()
    {
        await using var fixture = await CreateFixtureAsync();

        var handler = new CreateServiceCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            new StubAuthorizationService(
                PermissionScopeType.Branch));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(
                new CreateServiceCommand(
                    Guid.NewGuid(),
                    "Denied",
                    null,
                    30,
                    0),
                CancellationToken.None));
    }

    [Fact]
    public async Task DeleteCategory_WithServices_ReturnsConflict()
    {
        await using var fixture = await CreateFixtureAsync();

        var category = new ServiceCategory(
            fixture.TenantId,
            "Hair");

        var service = new ServiceEntity(
            fixture.TenantId,
            category.Id,
            "Haircut",
            45,
            1_000_000m);

        fixture.Db.ServiceCategories.Add(category);
        fixture.Db.Services.Add(service);
        await fixture.Db.SaveChangesAsync();

        var handler =
            new DeleteServiceCategoryCommandHandler(
                fixture.Db,
                fixture.TenantContext,
                fixture.TenantAuthorization);

        var result = await handler.Handle(
            new DeleteServiceCategoryCommand(
                category.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "service_category.in_use",
            result.Error.Code);
    }

    [Fact]
    public async Task ServiceList_FiltersByCategoryAndStatus()
    {
        await using var fixture = await CreateFixtureAsync();

        var hair = new ServiceCategory(
            fixture.TenantId,
            "Hair");

        var nails = new ServiceCategory(
            fixture.TenantId,
            "Nails");

        fixture.Db.ServiceCategories.AddRange(
            hair,
            nails);

        fixture.Db.Services.AddRange(
            new ServiceEntity(
                fixture.TenantId,
                hair.Id,
                "Haircut",
                45,
                1_000_000m),
            new ServiceEntity(
                fixture.TenantId,
                hair.Id,
                "Color",
                90,
                2_000_000m,
                isActive: false),
            new ServiceEntity(
                fixture.TenantId,
                nails.Id,
                "Manicure",
                60,
                800_000m));

        await fixture.Db.SaveChangesAsync();

        var handler = new ServiceListQueryHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        var result = await handler.Handle(
            new ServiceListQuery
            {
                CategoryId = hair.Id,
                IsActive = true,
                PageNumber = 1,
                PageSize = 20
            },
            CancellationToken.None);

        var item = Assert.Single(result.Data);
        Assert.Equal("Haircut", item.Name);
        Assert.Equal(hair.Id, item.CategoryId);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task ServiceState_CanDeactivateExistingService()
    {
        await using var fixture = await CreateFixtureAsync();

        var category = new ServiceCategory(
            fixture.TenantId,
            "Hair");

        var service = new ServiceEntity(
            fixture.TenantId,
            category.Id,
            "Haircut",
            45,
            1_000_000m);

        fixture.Db.ServiceCategories.Add(category);
        fixture.Db.Services.Add(service);
        await fixture.Db.SaveChangesAsync();

        var handler = new SetServiceActiveCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        await handler.Handle(
            new SetServiceActiveCommand(
                service.Id,
                false),
            CancellationToken.None);

        Assert.False(service.IsActive);
    }

    [Fact]
    public async Task CreateServiceValidator_RejectsInvalidDurationAndPrice()
    {
        var validator =
            new CreateServiceCommandValidator();

        var result = await validator.ValidateAsync(
            new CreateServiceCommand(
                Guid.NewGuid(),
                "Invalid",
                null,
                0,
                -1));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            x => x.PropertyName ==
                nameof(CreateServiceCommand.DurationMinutes));

        Assert.Contains(
            result.Errors,
            x => x.PropertyName ==
                nameof(CreateServiceCommand.Price));
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
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var interceptor =
            new ServicesTenantSaveChangesInterceptor(
                tenantContext);

        var options =
            new DbContextOptionsBuilder<ServicesDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(interceptor)
                .Options;

        var db = new TestServicesDbContext(
            options,
            tenantContext);

        await db.Database.EnsureCreatedAsync();

        return new Fixture(
            connection,
            db,
            tenantContext,
            tenantId,
            userId);
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
        SqliteConnection connection,
        ServicesDbContext db,
        TenantContext tenantContext,
        Guid tenantId,
        Guid userId)
        : IAsyncDisposable
    {
        public ServicesDbContext Db { get; } = db;
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
