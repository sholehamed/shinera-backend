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

The migrator applies catalog migrations first, then workspace registration migrations, using separate migration-history tables. Review it against your target database before running it. Frontend `npm start` proxies `/api` to port 5080. Production should reverse-proxy `/api` on the frontend origin. HTTP redirect behavior depends on the host's HTTPS configuration.

`GET /api/public/plans` returns `{ success: true, data: [...], error: null }`. A fresh database returns `data: []`. Add approved catalog data via the forthcoming authenticated administration workflow; there is no public write endpoint or speculative pricing seed.

To inspect migrations with a local `dotnet-ef` 10.x installation:

```sh
dotnet ef migrations has-pending-model-changes --project ShineraApp.Infrastructure --startup-project ShineraApp.Infrastructure --context PlanCatalogDbContext
dotnet ef migrations script --project ShineraApp.Infrastructure --startup-project ShineraApp.Infrastructure --context PlanCatalogDbContext
```

SQL Server is the deployment provider. SQLite tests validate relational projections, filtering and unique constraints but do not validate SQL Server-generated rowversion, concurrent registration/deadlocks or migration execution.

## Optional monitoring

After making `AppMonitoring.AspNetCore` 0.1.0 available in an authorized NuGet source:

```sh
dotnet build ShineraApp.slnx -p:EnableAppMonitoring=true
```

Monitoring is off by default because that package is unavailable from nuget.org. No fake replacement package is used.

## Registration

`POST /api/public/registrations` is disabled by default. For isolated development, explicitly set `Registration__Enabled=true` after reviewing approved zero-IRR catalog data. Catalog prices expose `canRegister` only for this configuration and a zero IRR amount. Do not enable production registration before verification/login, abuse controls and SQL Server checks are complete.

The endpoint validates the full owner/business/legal payload, resolves plan/price again inside a transaction, rejects paid plans, and returns only tenant ID, main branch ID and slug. It does not log in the user. Retrying an unchanged request must reuse `requestId`; editing any field requires a new ID.

27 tests pass: the existing 11 catalog tests plus 16 registration cases covering Solo/Salon provisioning, password hashing, unverified identity, idempotent replay, conflicting replay, disabled/paid rejection, changed server price, invalid input, duplicate identities/slugs, rollback after save and HTTP 429. Fixtures are isolated SQLite databases, not production seeds.

Check both contexts with `dotnet ef migrations has-pending-model-changes` and generate registration SQL using `--context RegistrationDbContext --project ShineraApp.Infrastructure --startup-project ShineraApp.Infrastructure`. Neither migration has been applied to a live SQL Server in this workspace.
