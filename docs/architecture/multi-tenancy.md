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

For now, API requests resolve a tenant from the `X-Tenant` HTTP header. The
header value is the practice slug, for example:

```http
X-Tenant: downtown-family-clinic
```

`GET /api/practices/{slug}` and `PUT /api/practices/{slug}` require this header.
The route slug must identify a Practice visible inside the tenant selected by
the header; a different tenant's row is excluded by the database query filter.

The header is only a tenant-routing mechanism. It is not authentication or an
authorization boundary by itself, so callers must not be considered trusted
solely because they supplied a tenant slug. Authentication and authorization
will be added with Keycloak.

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

## Planned resolution strategy

The header strategy is intentionally temporary. Tenant resolution will move to
subdomain routing and/or a trusted tenant claim in the authenticated JWT. The
tenant identifier and PostgreSQL store model can remain unchanged when the
strategy changes; only the request-resolution configuration and related edge
validation should need to change.
