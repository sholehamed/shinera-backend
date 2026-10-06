using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Modules.System.Appointments;
using Modules.System.Appointments.Infrastructure.Persistence.Contexts;
using Modules.System.Crm;
using Modules.System.Crm.Infrastructure.Persistence.Contexts;
using Modules.System.Identity;
using Modules.System.Identity.Infrastructure.Persistence;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;
using Modules.System.Subscription;
using Modules.System.Subscription.Infrastructure.Persistence;
using Modules.System.Subscription.Infrastructure.Persistence.Contexts;
using Modules.System.Services;
using Modules.System.Services.Infrastructure.Persistence.Contexts;
using Modules.System.Workforce;
using Modules.System.Workforce.Infrastructure.Persistence.Contexts;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentityModule(
    builder.Configuration,
    builder.Environment);
builder.Services.AddSubscriptionModule(builder.Configuration);
builder.Services.AddServicesModule(builder.Configuration);
builder.Services.AddWorkforceModule(builder.Configuration);
builder.Services.AddCrmModule(builder.Configuration);
builder.Services.AddAppointmentsModule(builder.Configuration);

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();

var identityDbContext =
    scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

await identityDbContext.Database.MigrateAsync();

var subscriptionDbContext =
    scope.ServiceProvider.GetRequiredService<SubscriptionDbContext>();

await subscriptionDbContext.Database.MigrateAsync();

var servicesDbContext =
    scope.ServiceProvider.GetRequiredService<ServicesDbContext>();

await servicesDbContext.Database.MigrateAsync();

var workforceDbContext =
    scope.ServiceProvider.GetRequiredService<WorkforceDbContext>();

await workforceDbContext.Database.MigrateAsync();

var crmDbContext =
    scope.ServiceProvider.GetRequiredService<CrmDbContext>();

await crmDbContext.Database.MigrateAsync();

var appointmentsDbContext =
    scope.ServiceProvider.GetRequiredService<AppointmentsDbContext>();

await appointmentsDbContext.Database.MigrateAsync();

var subscriptionCatalogSeeder =
    scope.ServiceProvider.GetRequiredService<SubscriptionCatalogSeedContributor>();

await subscriptionCatalogSeeder.SeedAsync();

var permissionCatalogSeeder =
    scope.ServiceProvider.GetRequiredService<SystemPermissionCatalogSeedContributor>();

await permissionCatalogSeeder.SeedAsync(scope.ServiceProvider);

var clientSeeder =
    scope.ServiceProvider.GetRequiredService<OpenIddictWebClientSeedContributor>();

await clientSeeder.SeedAsync(scope.ServiceProvider);

Console.WriteLine(
    "Identity, Subscription, Services, Workforce, CRM, and Appointments migrations applied; plan catalog, system permission catalog, and Shinera web OpenIddict client synchronized.");
