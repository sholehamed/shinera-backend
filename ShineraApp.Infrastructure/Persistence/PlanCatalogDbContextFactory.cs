using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShineraApp.Infrastructure.Persistence;

public sealed class PlanCatalogDbContextFactory : IDesignTimeDbContextFactory<PlanCatalogDbContext>
{
    public PlanCatalogDbContext CreateDbContext(string[] args)
    {
        // A connection is not opened when generating a migration.
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Shinera")
            ?? "Server=localhost;Database=Shinera;Integrated Security=true;TrustServerCertificate=true";
        return new(new DbContextOptionsBuilder<PlanCatalogDbContext>().UseSqlServer(connection).Options);
    }
}
