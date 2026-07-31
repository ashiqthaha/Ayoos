# Multi-tenancy

## Tenant model

An Ayoos tenant is a `Practice` (clinic). Each practice has a globally unique,
lowercase kebab-case slug. Finbuckle stores a corresponding `TenantInfo` record
in PostgreSQL:

- `TenantInfo.Id` is the practice `Guid` rendered as a string.
- `TenantInfo.Identifier` is the practice slug.
- `TenantInfo.Name` is the practice name.

The tenant registry is control-plane data and is not tenant-filtered. The
`Practice` row is tenant-owned data. Its Finbuckle shadow `TenantId` column is
set to the same practice ID when the practice is provisioned.

`POST /api/practices` is the bootstrap path and does not require an existing
tenant. It creates the tenant registration and then creates the Practice row in
a database context explicitly bound to that new tenant. If writing the Practice
fails, the new tenant registration is removed.

## Current request resolution

API authentication runs before tenant resolution. If a validated token contains
a `practice` or `tenant` claim, its value is tried first as the practice slug.
The `X-Tenant` HTTP header remains the fallback, for example:

```http
X-Tenant: downtown-family-clinic
```

`GET /api/practices/{slug}` and `PUT /api/practices/{slug}` require this header.
The route slug must identify a Practice visible inside the tenant selected by
the header; a different tenant's row is excluded by the database query filter.

The claim and header are tenant-routing mechanisms. Role-based authorization is
enforced separately from the validated Keycloak token, so callers must not be
considered authorized solely because they supplied a tenant slug.

## Data isolation

`AyoosDbContext` derives from Finbuckle's `MultiTenantDbContext`. Practice is
configured with `IsMultiTenant()`, which adds a `TenantId` shadow property and a
global EF Core query filter. Finbuckle also enforces the tenant ID during
inserts, updates, and deletes.

Every future tenant-owned EF Core entity must be explicitly configured with
`IsMultiTenant()` (or the equivalent Finbuckle attribute) in addition to its
normal entity mapping. Shared control-plane or reference entities must remain
explicitly non-tenant-owned. Code must not use `IgnoreQueryFilters` in normal
tenant request paths.

Tenant registrations and application data use the same PostgreSQL database but
separate EF Core contexts and migrations:

- `TenantStoreDbContext` owns Finbuckle's `TenantInfo` table.
- `AyoosDbContext` owns the `Practices` table and other tenant data.

Both migration sets are applied automatically at API startup only when the
environment is `Development`.

See [Authentication and authorization](authentication.md) for the Keycloak
token flow, realm roles, and tenant-claim mapper.
