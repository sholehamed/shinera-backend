using Infrastructure.SharedKernel.Persistence.Interceptors;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Data;

namespace Infrastructure.SharedKernel;

public static class DependencyInjection
{
    public static void AddBaseInfrastructureServices<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string appName,
        Action<IServiceProvider, DbContextOptionsBuilder>? configureOptions = null)
        where TDbContext : DbContext
    {
        var databaseName = configuration["DBNAME"];

        var connectionString =
            $"Server={Environment.GetEnvironmentVariable("DataBaseServer")};" +
            $"Database={databaseName};" +
            $"User Id={Environment.GetEnvironmentVariable("DataBaseUsername")};" +
            $"password={Environment.GetEnvironmentVariable("DataBasePassword")};" +
            "MultipleActiveResultSets=true;TrustServerCertificate=True";

        // All module DbContexts target the same Shinera database. Sharing one
        // scoped SqlConnection allows cross-module workflows (registration)
        // to enlist multiple DbContexts in one local SQL transaction without
        // escalating to a distributed transaction.
        services.TryAddScoped<SqlConnection>(
            _ => new SqlConnection(connectionString));

        services.AddTransient<IDbConnection>(
            _ => new SqlConnection(connectionString));

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<TDbContext>((sp, options) =>
        {
            var connection = sp.GetRequiredService<SqlConnection>();

            options.AddInterceptors(
                sp.GetServices<ISaveChangesInterceptor>());

            options.UseSqlServer(
                connection,
                sql => sql
                    .MigrationsHistoryTable($"__EFMigrationsHistory_{appName}")
                    .MigrationsAssembly(typeof(TDbContext).Assembly.FullName));

            configureOptions?.Invoke(sp, options);
        });
    }
}
