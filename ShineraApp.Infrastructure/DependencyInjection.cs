using ShineraApp.Application.Features.Registration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShineraApp.Application.Interfaces;
using ShineraApp.Infrastructure.Persistence;

namespace ShineraApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPlanCatalog(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PlanCatalogDbContext>(options => options.UseSqlServer(
            configuration.GetConnectionString("Shinera")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Shinera before querying the catalog.")));
        services.AddScoped<IPlanCatalogDbContext>(sp => sp.GetRequiredService<PlanCatalogDbContext>());
        services.Configure<RegistrationOptions>(configuration.GetSection("Registration"));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IOwnerPasswordHasher, OwnerPasswordHasher>();
        services.AddDbContext<RegistrationDbContext>(options => options.UseSqlServer(
            configuration.GetConnectionString("Shinera")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Shinera before registering."),
            sql => sql.MigrationsHistoryTable("__RegistrationMigrationsHistory", "Workspace")));
        services.AddScoped<IRegistrationDbContext>(sp => sp.GetRequiredService<RegistrationDbContext>());
        return services;
    }
}
