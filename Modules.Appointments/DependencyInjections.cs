using Modules.System.Appointments.Application.Abstractions;
using Modules.System.Appointments.Application.Availability;
using Modules.System.Appointments.Infrastructure.Persistence.Contexts;
using Modules.System.Appointments.Infrastructure.Persistence.Interceptors;
using System.Reflection;

namespace Modules.System.Appointments;

public static class DependencyInjections
{
    public static IServiceCollection AddAppointmentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<
            AppointmentsTenantSaveChangesInterceptor>();

        services.AddBaseInfrastructureServices<AppointmentsDbContext>(
            configuration,
            "Appointments",
            (provider, options) =>
            {
                options.AddInterceptors(
                    provider.GetRequiredService<
                        AppointmentsTenantSaveChangesInterceptor>());
            });

        services.AddScoped<IAppointmentsDbContext>(
            provider =>
                provider.GetRequiredService<AppointmentsDbContext>());

        services.AddScoped<
            IAppointmentAvailabilityService,
            AppointmentAvailabilityService>();

        services.AddCustomCqrs(
            Assembly.GetExecutingAssembly());

        return services;
    }

    public static WebApplication UseAppointmentsModule(
        this WebApplication app,
        IConfiguration configuration)
    {
        app.MapEndpoints(
            $"{configuration["BackendPrefix"]}Appointments");

        return app;
    }
}
