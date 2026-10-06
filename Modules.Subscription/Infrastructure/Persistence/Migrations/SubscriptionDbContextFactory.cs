using Microsoft.EntityFrameworkCore.Design;
using Modules.System.Subscription.Infrastructure.Persistence.Contexts;

namespace Modules.System.Subscription.Infrastructure.Persistence.Migrations;

internal sealed class SubscriptionDbContextFactory
    : IDesignTimeDbContextFactory<SubscriptionDbContext>
{
    public SubscriptionDbContext CreateDbContext(string[] args)
    {
        const string connectionString =
            "Server=localhost;Database=Shinera.DesignTime;User Id=sa;Password=DesignTimeOnly!123;TrustServerCertificate=True";

        var options =
            new DbContextOptionsBuilder<SubscriptionDbContext>();

        options.UseSqlServer(
            connectionString,
            sql => sql
                .MigrationsHistoryTable(
                    "__EFMigrationsHistory_Subscription")
                .MigrationsAssembly(
                    typeof(SubscriptionDbContextFactory)
                        .Assembly.FullName));

        return new SubscriptionDbContext(
            options.Options,
            new DesignTimeCurrentTenant());
    }

    private sealed class DesignTimeCurrentTenant
        : ICurrentTenant
    {
        public Guid? TenantId => null;
        public IReadOnlyCollection<Guid> WritableTenantIds => [];
        public bool IsFilterDisabled => false;
    }
}
