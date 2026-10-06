using Modules.System.Workforce.Application.Abstractions;
using Modules.System.Workforce.Infrastructure.Persistence.Contexts;
using Modules.System.Workforce.Infrastructure.Persistence.Interceptors;
using System.Reflection;

namespace Modules.System.Workforce;

public static class DependencyInjections
{
    public static IServiceCollection AddWorkforceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<WorkforceTenantSaveChangesInterceptor>();

        services.AddBaseInfrastructureServices<WorkforceDbContext>(
            configuration,
            "Workforce",
            (provider, options) =>
            {
                options.AddInterceptors(
                    provider.GetRequiredService<
                        WorkforceTenantSaveChangesInterceptor>());
            });

        services.AddScoped<IWorkforceDbContext>(
            provider =>
                provider.GetRequiredService<WorkforceDbContext>());

        services.AddCustomCqrs(
            Assembly.GetExecutingAssembly());

        return services;
    }

    public static WebApplication UseWorkforceModule(
        this WebApplication app,
        IConfiguration configuration)
    {
        app.MapEndpoints(
            $"{configuration["BackendPrefix"]}Workforce");

        return app;
    }
}
