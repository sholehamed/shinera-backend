using Microsoft.EntityFrameworkCore.Design;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;

namespace Modules.System.Identity.Infrastructure.Persistence.Migrations;

internal sealed class AppDbContextBuilder
    : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        const string connectionString =
            "Server=localhost;Database=Shinera.DesignTime;User Id=sa;Password=DesignTimeOnly!123;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<IdentityDbContext>();

        options.UseSqlServer(
            connectionString,
            sql => sql
                .MigrationsHistoryTable("__EFMigrationsHistory_Identity")
                .MigrationsAssembly(
                    typeof(AppDbContextBuilder).Assembly.FullName));

        return new IdentityDbContext(
            options.Options,
            new TenantContext());
    }
}
