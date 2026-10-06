using Modules.System.Services.Application.Abstractions;
using Modules.System.Services.Infrastructure.Persistence.Contexts;
using Modules.System.Services.Infrastructure.Persistence.Interceptors;
using System.Reflection;

namespace Modules.System.Services;

public static class DependencyInjections
{
    public static IServiceCollection AddServicesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ServicesTenantSaveChangesInterceptor>();

        services.AddBaseInfrastructureServices<ServicesDbContext>(
            configuration,
            "Services",
            (provider, options) =>
            {
                options.AddInterceptors(
                    provider.GetRequiredService<
                        ServicesTenantSaveChangesInterceptor>());
            });

        services.AddScoped<IServiceCatalogDbContext>(
            provider =>
                provider.GetRequiredService<ServicesDbContext>());

        services.AddCustomCqrs(
            Assembly.GetExecutingAssembly());

        return services;
    }

    public static WebApplication UseServicesModule(
        this WebApplication app,
        IConfiguration configuration)
    {
        app.MapEndpoints(
            $"{configuration["BackendPrefix"]}Services");

        return app;
    }
}
