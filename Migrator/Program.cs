using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Modules.System.Identity;
using Modules.System.Identity.Infrastructure.Persistence;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentityModule(builder.Configuration);

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();

var dbContext =
    scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

await dbContext.Database.MigrateAsync();

var clientSeeder =
    scope.ServiceProvider.GetRequiredService<OpenIddictWebClientSeedContributor>();

await clientSeeder.SeedAsync(scope.ServiceProvider);

Console.WriteLine(
    "Identity migrations applied and Shinera web OpenIddict client synchronized.");
