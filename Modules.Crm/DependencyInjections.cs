using Modules.System.Crm.Application.Abstractions;
using Modules.System.Crm.Infrastructure.Persistence.Contexts;
using Modules.System.Crm.Infrastructure.Persistence.Interceptors;
using System.Reflection;

namespace Modules.System.Crm;

public static class DependencyInjections
{
    public static IServiceCollection AddCrmModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<CrmTenantSaveChangesInterceptor>();

        services.AddBaseInfrastructureServices<CrmDbContext>(
            configuration,
            "Crm",
            (provider, options) =>
            {
                options.AddInterceptors(
                    provider.GetRequiredService<
                        CrmTenantSaveChangesInterceptor>());
            });

        services.AddScoped<ICrmDbContext>(
            provider =>
                provider.GetRequiredService<CrmDbContext>());

        services.AddCustomCqrs(
            Assembly.GetExecutingAssembly());

        return services;
    }

    public static WebApplication UseCrmModule(
        this WebApplication app,
        IConfiguration configuration)
    {
        app.MapEndpoints(
            $"{configuration["BackendPrefix"]}Crm");

        return app;
    }
}
