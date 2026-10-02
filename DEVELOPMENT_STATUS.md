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

## Registration foundation completed

- Config-gated public registration creates a hashed, unverified owner account, tenant, main branch, business profile, tenant/branch memberships, zero-price subscription and replay receipt atomically.
- Server-side input/pricing validation, serializable transaction, unique constraints, idempotency and per-IP rate limiting. No client tenant/owner/branch IDs or amount are accepted.
- Separate Workspace schema/migration; migrator applies Catalog before Workspace. Registration is off by default. No commercial values or free plans are seeded.
- Frontend submits the actual API request only for server-enabled zero-price IRR plans and handles success/errors/retries without logging credentials or issuing pretend login/payment success.

## Still missing / release gates

- OpenIddict login/token issuance, contact verification, authenticated tenant/branch resolution and permissions, subscription entitlement enforcement and renewals. This is a provisioning foundation, not a complete authentication system.
- Verified payment-provider integration; paid registration stays blocked. Authenticated catalog administration and approved production catalog values.
- Services, staff, customers, booking and VIP special requests.
- Live SQL Server migration/concurrency validation. SQLite does not prove SQL Server rowversion or concurrent-request behavior.

## Verification

- .NET build passed; 27 backend tests passed (11 catalog + 16 registration).
- Both EF models checked against migrations; SQL generation checked. No live database migrated.
- Paired frontend: 18 tests and development compilation passed. Existing production size-budget failures remain.
- Playwright discovers 14 desktop/mobile cases; browser execution is unverified because the Chromium download returned an invalid archive in this environment.
