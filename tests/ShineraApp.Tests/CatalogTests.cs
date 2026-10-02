using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShineraApp.Application.Features.Plans;
using ShineraApp.Application.Interfaces;
using ShineraApp.Domain;
using ShineraApp.Domain.Entities;
using ShineraApp.Infrastructure.Persistence;
using Web.Sharedkernel.Util;
using Xunit;

namespace ShineraApp.Tests;

public sealed class CatalogTestDbContext(DbContextOptions<CatalogTestDbContext> options)
    : DbContext(options), IPlanCatalogDbContext
{
    public DbSet<Plan> Plans => Set<Plan>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanCatalogDbContext).Assembly, type => type.Namespace == "ShineraApp.Infrastructure.Persistence.Configurations");
        // SQLite has no SQL Server rowversion generator; only the test provider varies.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            modelBuilder.Entity(entity.ClrType).Property("RowVersion").IsRequired(false);
    }
}

public sealed class CatalogFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public CatalogFactory() => connection.Open();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPlanCatalogDbContext>();
            services.AddDbContext<CatalogTestDbContext>(o => o.UseSqlite(connection));
            services.AddScoped<IPlanCatalogDbContext>(sp => sp.GetRequiredService<CatalogTestDbContext>());
        });
    }
    public async Task Seed(Action<CatalogTestDbContext> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogTestDbContext>();
        await db.Database.EnsureCreatedAsync();
        seed(db);
        await db.SaveChangesAsync();
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}

public sealed class CatalogTests
{
    [Fact]
    public void Prices_reject_negative_amounts_during_creation_and_changes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlanPrice(Guid.NewGuid(), BillingPeriod.Monthly, -1, "IRR"));
        var price = new PlanPrice(Guid.NewGuid(), BillingPeriod.Monthly, 0, "IRR");
        Assert.Throws<ArgumentOutOfRangeException>(() => price.ChangeAmount(-1));
        Assert.Equal(0, price.Amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("IR")]
    [InlineData("123")]
    [InlineData("IRRR")]
    public void Prices_reject_invalid_currency_codes(string currency)
        => Assert.ThrowsAny<ArgumentException>(() => new PlanPrice(Guid.NewGuid(), BillingPeriod.Monthly, 0, currency));

    [Fact]
    public void Prices_reject_missing_plan_and_unknown_billing_period()
    {
        Assert.Throws<ArgumentException>(() => new PlanPrice(Guid.Empty, BillingPeriod.Monthly, 0, "IRR"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlanPrice(Guid.NewGuid(), (BillingPeriod)99, 0, "IRR"));
        Assert.Equal("IRR", new PlanPrice(Guid.NewGuid(), BillingPeriod.Monthly, 0, " irr ").Currency);
    }

    [Fact]
    public async Task Anonymous_endpoint_returns_empty_success_when_no_plans_exist()
    {
        using var factory = new CatalogFactory();
        await factory.Seed(_ => { });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync("/api/public/plans");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PublicPlanDto[]>>();
        Assert.True(body!.Success);
        Assert.Empty(body.Data!);
    }

    [Fact]
    public async Task Excludes_private_inactive_and_deleted_plans_and_orders_public_plans()
    {
        using var factory = new CatalogFactory();
        await factory.Seed(db =>
        {
            var first = new Plan("solo", "انفرادی", PlanAudience.Solo); first.Configure(0, 1);
            var last = new Plan("salon", "سالن", PlanAudience.Salon); last.Configure(0, 2);
            var inactive = new Plan("inactive", "Inactive", PlanAudience.Solo); inactive.SetActive(false);
            var hidden = new Plan("private", "Private", PlanAudience.Solo);
            hidden.Update("Private", null, PlanAudience.Solo, 0, 0, false);
            var deleted = new Plan("deleted", "Deleted", PlanAudience.Solo); deleted.Delete(Guid.NewGuid(), "127.0.0.1");
            db.AddRange(last, inactive, first, hidden, deleted);
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var body = await client.GetFromJsonAsync<ApiResponse<PublicPlanDto[]>>("/api/public/plans?tenantId=untrusted");
        Assert.Equal(new[] { "solo", "salon" }, body!.Data!.Select(x => x.Key));
    }

    [Fact]
    public async Task Returns_only_active_prices_and_visible_enabled_features_without_currency_conversion()
    {
        using var factory = new CatalogFactory();
        await factory.Seed(db =>
        {
            var plan = new Plan("solo", "انفرادی", PlanAudience.Solo);
            var active = new PlanPrice(plan.Id, BillingPeriod.Monthly, 6900000m, "irr");
            var inactive = new PlanPrice(plan.Id, BillingPeriod.Yearly, 100m, "IRR"); inactive.SetActive(false);
            var deletedPrice = new PlanPrice(plan.Id, BillingPeriod.Monthly, 20m, "USD"); deletedPrice.Delete(Guid.NewGuid(), "127.0.0.1");
            db.AddRange(plan, active, inactive, deletedPrice);
            foreach (var code in new[] { "visible", "hidden", "inactive", "disabled", "deleted", "deleted-link" })
            {
                var feature = new Feature(code, code, FeatureValueType.Toggle);
                if (code == "hidden") feature.Update(code, null, null, 0, false);
                if (code == "inactive") feature.SetActive(false);
                if (code == "deleted") feature.Delete(Guid.NewGuid(), "127.0.0.1");
                var link = new PlanFeature(plan.Id, feature.Id, code != "disabled");
                if (code == "deleted-link") link.Delete(Guid.NewGuid(), "127.0.0.1");
                db.AddRange(feature, link);
            }
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var body = await client.GetFromJsonAsync<ApiResponse<PublicPlanDto[]>>("/api/public/plans");
        var result = Assert.Single(body!.Data!);
        var price = Assert.Single(result.Prices);
        Assert.Equal(6900000m, price.Amount); Assert.Equal("IRR", price.Currency); Assert.Equal("monthly", price.BillingCycle);
        Assert.Equal("visible", Assert.Single(result.Features).Code);
    }

    [Fact]
    public async Task Query_is_read_only_and_cancellation_is_propagated()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var db = new CatalogTestDbContext(new DbContextOptionsBuilder<CatalogTestDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.Add(new Plan("solo", "Solo", PlanAudience.Solo)); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var handler = new GetPublicPlansHandler(db);
        var result = await handler.Handle(new(), CancellationToken.None);
        Assert.True(result.IsSuccess); Assert.Empty(db.ChangeTracker.Entries());
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.Handle(new(), cts.Token));
    }

    [Fact]
    public async Task Database_rejects_duplicate_active_price_for_same_plan_period_and_currency()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        using var db = new CatalogTestDbContext(new DbContextOptionsBuilder<CatalogTestDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var plan = new Plan("solo", "Solo", PlanAudience.Solo);
        db.AddRange(plan, new PlanPrice(plan.Id, BillingPeriod.Monthly, 0, "IRR"), new PlanPrice(plan.Id, BillingPeriod.Monthly, 1, "IRR"));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
