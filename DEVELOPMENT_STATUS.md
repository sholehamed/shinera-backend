# Development status — 2026-10-02

## Completed in this slice

- Public catalog: `GET /api/public/plans` through the existing custom CQRS dispatcher and `ApiResponse<T>` envelope.
- SQL Server catalog context, configurations, soft-delete filters and generated EF migration for existing Plan/Price/Feature entities.
- Public queries exclude private, inactive and deleted plans; inactive/deleted prices; disabled, hidden, inactive and deleted features/links.
- Monetary values retain their stored currency and amount. Price constructors reject negative amounts, missing plans, unknown billing periods and malformed currencies.
- Explicit migrator executable; no schema mutation during API startup.
- 11 passing tests covering domain invariants, anonymous HTTP responses, relational filtering, unique active prices, read-only queries and cancellation.
- Build restored by making the unavailable AppMonitoring package opt-in. Backend CI added.

## Actual repository baseline

The initial GitHub snapshot had shared CQRS/Result/entity abstractions and Tenant/Branch/Plan domain classes. Application features, application persistence, migrations and test implementations were absent. The host had a sample weather endpoint and cookie authentication; it did not have working OpenIddict registration/login. These are not marked complete based on older external descriptions.

## Next: registration foundation

Implement identity/owner, tenant and main-branch persistence/memberships, registration draft validation, transaction/idempotency and verified payment integration before enabling checkout. Keep UI and API permissions separate and enforce tenant ownership server-side. Build this with the existing CQRS and Result contracts; do not add a parallel mediator.

## Still missing

- Authenticated plan administration and approved production prices/feature assignments. The catalog intentionally returns an empty list on a new database rather than inventing commercial prices.
- Real registration, OpenIddict login, subscriptions, onboarding, services, staff, customers, booking, VIP special requests and tenant/branch authorization.
- SQL Server execution test for the generated migration. Relational tests use SQLite and do not prove SQL Server rowversion/concurrency behavior.

## Verification

- `dotnet build ShineraApp.slnx -m:1`: passed.
- `dotnet test tests/ShineraApp.Tests/ShineraApp.Tests.csproj -m:1`: 11 passed.
- EF model/migration consistency and SQL generation checked; no live database was migrated.
- Paired frontend: development build and 14 unit tests passed. Production build has pre-existing size-budget errors. Playwright cases exist, but browser download was unavailable in this workspace.
