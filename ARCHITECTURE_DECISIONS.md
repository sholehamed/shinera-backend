# Decisions — public plan catalog

1. Preserve the current flat project layout and custom CQRS. The supplied backend guide's module paths are examples, not grounds for restructuring the repository.
2. Catalog data is platform-owned reference data, so public catalog requests accept no tenant/branch selector. This is not a bypass for future tenant-owned business data.
3. Use the existing rich domain classes and full audit configurations. The new context handles catalog tables only; it must not be reused to expose tenant-owned data without tenancy enforcement. This slice exposes no write endpoint and does not replace audit interceptors for future writes.
4. Publish only visible enabled features and active prices. Do not seed assumed paid prices or grant entitlements based on UI data. Future checkout must resolve and validate the selected price again on the server.
5. Keep amounts in native currency (IRR). Frontend displays these values as ریال with no implicit تومان conversion. Missing prices are distinct from zero.
6. Preserve the AppMonitoring integration behind `EnableAppMonitoring=true`; the package is not published on the configured public feed and no package source was supplied. Core business functionality must build without it.
7. Registration/payment remains explicitly unavailable until owner creation, tenant/branch memberships and provider-verified, idempotent payment completion exist. Remove mock success and credential logging immediately.
8. SQLite tests reuse production entity configurations, relaxing rowversion generation only in the test provider. SQL Server migration/runtime verification remains a separate deployment gate.
