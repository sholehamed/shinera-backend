using Infrastructure.SharedKernel;
using Infrastructure.SharedKernel.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests.Infrastructure;

public sealed class InfrastructureRegistrationTests
{
    [Fact]
    public void BaseInfrastructureRegistration_IsIdempotentForSharedInterceptors()
    {
        var services = new ServiceCollection();
        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["DBNAME"] = "shinera-test"
                    })
                .Build();

        services.AddBaseInfrastructureServices<FirstDbContext>(
            configuration,
            "First");

        services.AddBaseInfrastructureServices<SecondDbContext>(
            configuration,
            "Second");

        services.AddBaseInfrastructureServices<ThirdDbContext>(
            configuration,
            "Third");

        var interceptorDescriptors = services
            .Where(descriptor =>
                descriptor.ServiceType ==
                    typeof(ISaveChangesInterceptor))
            .ToList();

        Assert.Single(
            interceptorDescriptors.Where(
                x => x.ImplementationType ==
                    typeof(AuditableEntityInterceptor)));

        Assert.Single(
            interceptorDescriptors.Where(
                x => x.ImplementationType ==
                    typeof(DispatchDomainEventsInterceptor)));
    }

    private sealed class FirstDbContext(
        DbContextOptions<FirstDbContext> options)
        : DbContext(options);

    private sealed class SecondDbContext(
        DbContextOptions<SecondDbContext> options)
        : DbContext(options);

    private sealed class ThirdDbContext(
        DbContextOptions<ThirdDbContext> options)
        : DbContext(options);
}
