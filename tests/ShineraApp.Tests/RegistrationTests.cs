using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ShineraApp.Application.Features.Registration;
using ShineraApp.Application.Interfaces;
using ShineraApp.Domain;
using ShineraApp.Domain.Entities;
using ShineraApp.Infrastructure;
using ShineraApp.Infrastructure.Persistence;
using Web.Sharedkernel.Util;
using Xunit;

namespace ShineraApp.Tests;

public sealed class RegistrationTestDb(DbContextOptions<RegistrationTestDb> options)
    : DbContext(options), IRegistrationDbContext, IPlanCatalogDbContext
{
    public bool FailAfterSave { get; set; }
    public DbSet<OwnerAccount> Owners => Set<OwnerAccount>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<BranchMembership> BranchMemberships => Set<BranchMembership>();
    public DbSet<TenantSubscription> Subscriptions => Set<TenantSubscription>();
    public DbSet<WorkspaceRegistration> Registrations => Set<WorkspaceRegistration>();
    public DbSet<Plan> Plans => Set<Plan>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(RegistrationDbContext).Assembly);
        foreach (var entity in builder.Model.GetEntityTypes())
            builder.Entity(entity.ClrType).Property("RowVersion").IsRequired(false);
    }
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var count = await base.SaveChangesAsync(ct);
        if (FailAfterSave) throw new RegistrationConflictException();
        return count;
    }
}

public sealed class RegistrationFactory(bool enabled = true) : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services => {
            services.RemoveAll<IRegistrationDbContext>(); services.RemoveAll<IPlanCatalogDbContext>();
            services.AddDbContext<RegistrationTestDb>(o => o.UseSqlite(connection));
            services.AddScoped<IRegistrationDbContext>(sp => sp.GetRequiredService<RegistrationTestDb>());
            services.AddScoped<IPlanCatalogDbContext>(sp => sp.GetRequiredService<RegistrationTestDb>());
            services.PostConfigure<RegistrationOptions>(o => o.Enabled = enabled);
        });
    }
    public async Task Seed(decimal amount = 0, PlanAudience audience = PlanAudience.Solo)
    {
        using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<RegistrationTestDb>();
        await db.Database.EnsureCreatedAsync();
        var plan = new Plan("solo", "Solo", audience);
        db.AddRange(plan, new PlanPrice(plan.Id, BillingPeriod.Monthly, amount, "IRR"));
        await db.SaveChangesAsync();
    }
    public HttpClient Client() => CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
}

public sealed class RegistrationTests
{
    private static RegisterWorkspaceCommand Valid() => new(Guid.NewGuid(), "solo", "monthly",
        "First", "Last", "owner@example.test", "09123456789", "a-long-test-password",
        "Beauty", "beauty-salon", "salon", "", "Tehran", "Address", "", "", true, true);

    [Theory]
    [InlineData(PlanAudience.Solo, TenantType.Solo)]
    [InlineData(PlanAudience.Salon, TenantType.Salon)]
    public async Task Registers_owner_workspace_main_branch_memberships_and_subscription_atomically(PlanAudience audience, TenantType type)
    {
        using var factory = new RegistrationFactory(); await factory.Seed(audience: audience);
        using var client = factory.Client(); var request = Valid();
        var response = await client.PostAsJsonAsync("/api/public/registrations", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RegistrationReceipt>>();
        Assert.True(body!.Success);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<RegistrationTestDb>();
        var owner = await db.Owners.SingleAsync(); var tenant = await db.Tenants.SingleAsync();
        var branch = await db.Branches.SingleAsync();
        Assert.Equal(type, tenant.Type); Assert.Equal(tenant.Id, branch.TenantId); Assert.True(branch.IsMain);
        Assert.Equal(body.Data!.BranchId, branch.Id); Assert.Equal(owner.Id, tenant.CreatedBy);
        Assert.NotEqual(request.Password, owner.PasswordHash);
        Assert.True(new OwnerPasswordHasher().Verify(owner.PasswordHash, request.Password));
        Assert.False(owner.EmailVerified); Assert.False(owner.MobileVerified);
        Assert.Equal(tenant.Id, (await db.TenantMemberships.SingleAsync()).TenantId);
        Assert.Equal(branch.Id, (await db.BranchMemberships.SingleAsync()).BranchId);
        Assert.Equal(tenant.Id, (await db.BusinessProfiles.SingleAsync()).TenantId);
        var subscription = await db.Subscriptions.SingleAsync();
        Assert.Equal(0, subscription.Amount); Assert.Equal(subscription.StartsAt.AddMonths(1), subscription.ExpiresAt);
        Assert.DoesNotContain("password", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Replaying_same_request_returns_same_receipt_without_duplicate_data()
    {
        using var factory = new RegistrationFactory(); await factory.Seed(); using var client = factory.Client();
        var request = Valid(); var a = await client.PostAsJsonAsync("/api/public/registrations", request);
        var b = await client.PostAsJsonAsync("/api/public/registrations", request);
        Assert.Equal(await a.Content.ReadAsStringAsync(), await b.Content.ReadAsStringAsync());
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<RegistrationTestDb>();
        Assert.Equal(1, await db.Owners.CountAsync()); Assert.Equal(1, await db.Tenants.CountAsync()); Assert.Equal(1, await db.Subscriptions.CountAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Request_id_cannot_be_reused_with_other_data_or_password(bool changePassword)
    {
        using var factory = new RegistrationFactory(); await factory.Seed(); using var client = factory.Client();
        var request = Valid(); (await client.PostAsJsonAsync("/api/public/registrations", request)).EnsureSuccessStatusCode();
        var changed = changePassword ? request with { Password = "a-different-password" } : request with { Slug = "another-salon" };
        var response = await client.PostAsJsonAsync("/api/public/registrations", changed);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("Registration.RequestConflict", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(false, 0, "Registration.Unavailable")]
    [InlineData(true, 100, "Registration.PaymentRequired")]
    public async Task Disabled_registration_or_paid_plan_never_provisions(bool enabled, int amount, string error)
    {
        using var factory = new RegistrationFactory(enabled); await factory.Seed(amount); using var client = factory.Client();
        var response = await client.PostAsJsonAsync("/api/public/registrations", Valid());
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); Assert.Contains(error, await response.Content.ReadAsStringAsync());
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<RegistrationTestDb>();
        Assert.Equal(0, await db.Tenants.CountAsync()); Assert.Equal(0, await db.Owners.CountAsync());
    }

    [Fact]
    public async Task Changed_price_is_rechecked_on_the_server()
    {
        using var factory = new RegistrationFactory(); await factory.Seed(); using var client = factory.Client();
        var catalog = await client.GetAsync("/api/public/plans"); Assert.Contains("\"canRegister\":true", await catalog.Content.ReadAsStringAsync());
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<RegistrationTestDb>();
            (await db.Set<PlanPrice>().SingleAsync()).ChangeAmount(100); await db.SaveChangesAsync();
        }
        var response = await client.PostAsJsonAsync("/api/public/registrations", Valid());
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("slug")]
    [InlineData("terms")]
    [InlineData("email")]
    [InlineData("mobile")]
    public async Task Invalid_input_is_rejected_without_writes(string field)
    {
        using var factory = new RegistrationFactory(); await factory.Seed(); using var client = factory.Client();
        var request = field switch {
            "password" => Valid() with { Password = "short" }, "slug" => Valid() with { Slug = "admin" },
            "terms" => Valid() with { AcceptTerms = false }, "email" => Valid() with { Email = "invalid" },
            _ => Valid() with { Mobile = "123" }
        };
        var response = await client.PostAsJsonAsync("/api/public/registrations", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope(); Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<RegistrationTestDb>().Owners.CountAsync());
    }

    [Fact]
    public async Task Duplicate_email_mobile_or_slug_cannot_create_another_workspace()
    {
        using var factory = new RegistrationFactory(); await factory.Seed(); using var client = factory.Client();
        var first = Valid(); (await client.PostAsJsonAsync("/api/public/registrations", first)).EnsureSuccessStatusCode();
        foreach (var request in new[] {
            first with { RequestId = Guid.NewGuid(), Slug = "new-one", Mobile = "09111111111" },
            first with { RequestId = Guid.NewGuid(), Slug = "new-two", Email = "second@example.test" },
            first with { RequestId = Guid.NewGuid(), Email = "third@example.test", Mobile = "09222222222" } })
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/public/registrations", request)).StatusCode);
    }

    [Fact]
    public async Task Failure_after_database_save_rolls_back_every_provisioned_record()
    {
        using var factory = new RegistrationFactory(); await factory.Seed();
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<RegistrationTestDb>(); db.FailAfterSave = true;
            var handler = new RegisterWorkspaceHandler(db, new OwnerPasswordHasher(), Options.Create(new RegistrationOptions { Enabled = true }), TimeProvider.System);
            Assert.True((await handler.Handle(Valid(), default)).IsFailure);
        }
        using var check = factory.Services.CreateScope(); var persisted = check.ServiceProvider.GetRequiredService<RegistrationTestDb>();
        Assert.Equal(0, await persisted.Owners.CountAsync()); Assert.Equal(0, await persisted.Tenants.CountAsync());
        Assert.Equal(0, await persisted.Branches.CountAsync()); Assert.Equal(0, await persisted.Subscriptions.CountAsync());
        Assert.Equal(0, await persisted.Registrations.CountAsync());
    }

    [Fact]
    public async Task Rate_limit_rejects_repeated_registration_requests()
    {
        using var factory = new RegistrationFactory(false); await factory.Seed(); using var client = factory.Client();
        for (var i = 0; i < 5; i++) await client.PostAsJsonAsync("/api/public/registrations", Valid());
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/public/registrations", Valid())).StatusCode);
    }
}
