# Build, tests and local API

Requires .NET 10 SDK. Open `ShineraApp.slnx` in a current Visual Studio or run:

```sh
dotnet build ShineraApp.slnx -m:1
dotnet test tests/ShineraApp.Tests/ShineraApp.Tests.csproj -m:1
```

Tests use isolated in-memory SQLite databases and `WebApplicationFactory`; no user account, external service, production connection string or manual data reset is required. Test fixtures are not production seed data.

## SQL Server setup

Set `ConnectionStrings__Shinera` to a development SQL Server connection string in your shell (do not commit credentials). Then:

```sh
dotnet run --project ShineraApp.Migrator
dotnet run --project ShineraApp --no-launch-profile --urls http://localhost:5080
```

The migrator applies the checked-in catalog migration only. Review it against your target database before running it. Frontend `npm start` proxies `/api` to port 5080. Production should reverse-proxy `/api` on the frontend origin. HTTP redirect behavior depends on the host's HTTPS configuration.

`GET /api/public/plans` returns `{ success: true, data: [...], error: null }`. A fresh database returns `data: []`. Add approved catalog data via the forthcoming authenticated administration workflow; there is no public write endpoint or speculative pricing seed.

To inspect migrations with a local `dotnet-ef` 10.x installation:

```sh
dotnet ef migrations has-pending-model-changes --project ShineraApp.Infrastructure --startup-project ShineraApp.Infrastructure --context PlanCatalogDbContext
dotnet ef migrations script --project ShineraApp.Infrastructure --startup-project ShineraApp.Infrastructure --context PlanCatalogDbContext
```

SQL Server is the deployment provider. SQLite tests validate relational projections, filtering and unique constraints but do not validate SQL Server-generated rowversion or migration execution.

## Optional monitoring

After making `AppMonitoring.AspNetCore` 0.1.0 available in an authorized NuGet source:

```sh
dotnet build ShineraApp.slnx -p:EnableAppMonitoring=true
```

Monitoring is off by default because that package is unavailable from nuget.org. No fake replacement package is used.
