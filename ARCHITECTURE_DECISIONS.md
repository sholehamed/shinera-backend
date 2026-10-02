# Decisions — public plan catalog

1. Preserve the current flat project layout and custom CQRS. The supplied backend guide's module paths are examples, not grounds for restructuring the repository.
2. Catalog data is platform-owned reference data, so public catalog requests accept no tenant/branch selector. This is not a bypass for future tenant-owned business data.
3. Use the existing rich domain classes and full audit configurations. The new context handles catalog tables only; it must not be reused to expose tenant-owned data without tenancy enforcement. The catalog context exposes no writes; registration uses a separate bootstrap context and does not replace audit interceptors for future business writes.
4. Publish only visible enabled features and active prices. Do not seed assumed paid prices or grant entitlements based on UI data. Future checkout must resolve and validate the selected price again on the server.
5. Keep amounts in native currency (IRR). Frontend displays these values as ریال with no implicit تومان conversion. Missing prices are distinct from zero.
6. Preserve the AppMonitoring integration behind `EnableAppMonitoring=true`; the package is not published on the configured public feed and no package source was supplied. Core business functionality must build without it.
7. Registration is disabled by default. With `Registration:Enabled=true`, only an explicitly published, active zero-IRR price can provision a workspace. Paid plans remain blocked until provider-verified payment completion exists; no free plan is seeded and no payment success is simulated.
8. SQLite tests reuse production entity configurations, relaxing rowversion generation only in the test provider. SQL Server migration/runtime verification remains a separate deployment gate.

## Registration foundation

9. A separate privileged RegistrationDbContext creates owner, tenant, main branch, profile, both memberships, subscription and idempotency receipt in one serializable transaction. It accepts no client tenant/owner/branch IDs or amount. Never reuse it for tenant-facing reads; authenticated tenant/branch resolution and authorization are still required. Catalog migrations retain ownership of catalog tables; workspace migrations reference them without recreating them.
10. Owner credentials use the framework versioned PBKDF2 password hasher (210,000 iterations), without adding an Identity user store or issuing a token. Email/mobile remain unverified. Login/OpenIddict integration and verification must be implemented before production activation.
11. Request IDs are globally unique. Replay checks the normalized non-password fingerprint and verifies the submitted password against the stored salted hash. Secrets are neither fingerprinted with a fast hash nor returned or logged. Deleted identities/slugs stay reserved. A repeated successful request returns its original receipt.
12. Public bootstrap audit fields point to the newly created owner. Only the zero-price subscription is created, with the selected monthly/yearly duration. No renewal, trial billing, payment, entitlement enforcement or authenticated session is claimed.
13. The per-process registration limiter allows five requests/minute per remote IP. Production proxy configuration must trust only known proxies; multi-instance deployments need a shared edge limiter. SQL Server concurrent request/deadlock behavior remains a deployment test gate.
