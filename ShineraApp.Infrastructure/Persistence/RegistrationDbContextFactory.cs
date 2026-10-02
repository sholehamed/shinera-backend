using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace ShineraApp.Infrastructure.Persistence;
public sealed class RegistrationDbContextFactory : IDesignTimeDbContextFactory<RegistrationDbContext>
{
    public RegistrationDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<RegistrationDbContext>().UseSqlServer(
            Environment.GetEnvironmentVariable("ConnectionStrings__Shinera")
            ?? "Server=localhost;Database=Shinera;Integrated Security=true;TrustServerCertificate=true",
            sql => sql.MigrationsHistoryTable("__RegistrationMigrationsHistory", "Workspace")).Options);
}
