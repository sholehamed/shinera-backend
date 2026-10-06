using Application.SharedKernel.Abstractions;
using Application.SharedKernel.Abstractions.Mapping;
using Application.SharedKernel.Abstractions.Messaging;
using Application.SharedKernel.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using DispatcherClass = Application.SharedKernel.Dispatcher.Dispatcher;

namespace Application.SharedKernel;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomCqrs<TProfile>(
        this IServiceCollection services,
        params Assembly[] assemblies)
        where TProfile : MappingProfile, new()
    {
        services.AddAutoMapper(x => x.AddProfile<TProfile>());
        AddCoreServices(services, assemblies);
        return services;
    }

    public static IServiceCollection AddCustomCqrs(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        AddCoreServices(services, assemblies);
        return services;
    }

    private static void AddCoreServices(
        IServiceCollection services,
        Assembly[] assemblies)
    {
        services.TryAddSingleton<System.TimeProvider>(_ => System.TimeProvider.System);
        services.TryAddSingleton<ITimeZoneResolver, TimeZoneResolver>();

        // Compatibility adapter for legacy slices. New code should inject System.TimeProvider directly.
        services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddScoped<IDispatcher, DispatcherClass>();
        services.AddValidatorsFromAssemblies(assemblies);

        RegisterHandlers(services, assemblies);
    }

    private static void RegisterHandlers(
        IServiceCollection services,
        Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var @interface in type.GetInterfaces())
                {
                    if (!@interface.IsGenericType)
                        continue;

                    var genericTypeDefinition = @interface.GetGenericTypeDefinition();

                    if (genericTypeDefinition == typeof(ICommandHandler<,>) ||
                        genericTypeDefinition == typeof(ICommandHandler<>) ||
                        genericTypeDefinition == typeof(IQueryHandler<,>) ||
                        genericTypeDefinition == typeof(INotificationHandler<>) ||
                        genericTypeDefinition == typeof(IDomainEventHandler<>))
                    {
                        services.AddScoped(@interface, type);
                    }
                }
            }
        }
    }
}
