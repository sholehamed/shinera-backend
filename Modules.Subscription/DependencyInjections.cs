using Modules.System.Subscription.Application.Abstractions;
using Modules.System.Subscription.Application.Entitlements;
using Application.SharedKernel.Registration;
using Modules.System.Subscription.Application.Registration;
using Modules.System.Subscription.Infrastructure.Persistence.Contexts;
using Modules.System.Subscription.Infrastructure.Persistence.Interceptors;
using Modules.System.Subscription.Web.Authorization;

namespace Modules.System.Subscription;

public static class DependencyInjections
{
    public static IServiceCollection AddSubscriptionModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<SubscriptionTenantSaveChangesInterceptor>();

        services.AddBaseInfrastructureServices<SubscriptionDbContext>(
            configuration,
            "Subscription",
            (provider, options) =>
            {
                options.AddInterceptors(
                    provider.GetRequiredService<
                        SubscriptionTenantSaveChangesInterceptor>());
            });

        services.AddScoped<IEntitlementDbContext>(
            provider =>
                provider.GetRequiredService<SubscriptionDbContext>());

        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<IRegistrationSubscriptionProvisioner, RegistrationSubscriptionProvisioner>();
        services.AddScoped<IAuthorizationHandler, FeatureEntitlementAuthorizationHandler>();

        services.AddAuthorization();

        return services;
    }

    public static WebApplication UseSubscriptionModule(
        this WebApplication app,
        IConfiguration configuration)
    {
        app.MapEndpoints(
            $"{configuration["BackendPrefix"]}Subscription");

        return app;
    }
}
