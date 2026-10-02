using Microsoft.EntityFrameworkCore;
using ShineraApp.Infrastructure.Persistence;

var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Shinera");
if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("Set ConnectionStrings__Shinera before running the migrator.");

await using var db = new PlanCatalogDbContext(
    new DbContextOptionsBuilder<PlanCatalogDbContext>().UseSqlServer(connection).Options);
await db.Database.MigrateAsync();
await using var registration = new RegistrationDbContext(new DbContextOptionsBuilder<RegistrationDbContext>()
    .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__RegistrationMigrationsHistory", "Workspace")).Options);
await registration.Database.MigrateAsync();
Console.WriteLine("Catalog and registration migrations applied.");
