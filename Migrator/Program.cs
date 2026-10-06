using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Modules.System.Identity;
using Modules.System.Identity.Infrastructure.Persistence;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;
using Modules.System.Subscription;
using Modules.System.Subscription.Infrastructure.Persistence.Contexts;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddSubscriptionModule(builder.Configuration);

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();

var identityDbContext =
    scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

await identityDbContext.Database.MigrateAsync();

var subscriptionDbContext =
    scope.ServiceProvider.GetRequiredService<SubscriptionDbContext>();

await subscriptionDbContext.Database.MigrateAsync();

var clientSeeder =
    scope.ServiceProvider.GetRequiredService<OpenIddictWebClientSeedContributor>();

await clientSeeder.SeedAsync(scope.ServiceProvider);

Console.WriteLine(
    "Identity and Subscription migrations applied; Shinera web OpenIddict client synchronized.");
