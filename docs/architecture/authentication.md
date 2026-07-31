# Authentication and authorization

## Keycloak realm

Local Ayoos authentication is provided by the `ayoos` Keycloak realm. Its
complete development configuration is checked in at
`infra/keycloak/realm-ayoos.json` and imported automatically when Keycloak
starts. No Admin Console setup is required.

The realm contains two clients:

- `ayoos-frontend` is a public OpenID Connect browser client. It uses the
  authorization code flow with PKCE S256. The application callback, silent
  callback, and logout return paths are under `http://localhost:3000/*`.
- `ayoos-api` is a bearer-only resource server. An audience mapper on
  `ayoos-frontend` adds `ayoos-api` to frontend access tokens, allowing the API
  to validate them as its intended audience.

The frontend client also allows
`http://localhost:5000/swagger/oauth2-redirect.html` and the corresponding
origin in local development so Swagger's **Authorize** button can complete the
same PKCE flow. Production deployments should use environment-specific exact
redirect URIs and origins.

The realm roles are:

- `practice-admin`
- `provider`
- `staff`
- `patient`

The API maps the roles in Keycloak's `realm_access.roles` access-token claim to
ASP.NET role claims. It defines these authorization policies:

- `PracticeAdmin` requires `practice-admin`.
- `ProviderOnly` requires `provider`.
- `StaffOrAdmin` accepts `staff` or `practice-admin`.
- `AuthenticatedUser` accepts any authenticated realm user.

Practice and provider reads require `AuthenticatedUser`. Their write
operations require `StaffOrAdmin`. `/health` remains public.

## Browser token flow

The frontend uses `oidc-client-ts` because it is a small, framework-independent
OIDC client with first-class authorization-code/PKCE and automatic token-renewal
support. A server-session framework would add a separate application session
and callback API that Ayoos does not currently need.

1. An unauthenticated visit to `/practice/*` or `/setup/*` is redirected to
   `/login`.
2. **Sign in** starts an authorization-code request to Keycloak with a PKCE
   S256 challenge.
3. Keycloak returns the browser to `/auth/callback`; the frontend verifies the
   saved transaction state and exchanges the code with its PKCE verifier.
4. The resulting user and tokens are held only in an in-memory OIDC store.
   Temporary request state and the PKCE transaction are stored in
   `sessionStorage` only long enough to survive the redirect.
5. `oidc-client-ts` renews an expiring session automatically. On a full page
   reload, the app uses a silent OIDC request and the existing Keycloak session
   to reconstruct the in-memory session.
6. The API client attaches the access token as `Authorization: Bearer <token>`
   to every application API request. A `401` clears the local in-memory session
   and redirects to `/login`.
7. `/logout` starts Keycloak logout, which clears the identity-provider
   session and returns the browser to `/login`.

The browser-facing authority is
`http://localhost:8081/realms/ayoos`. Inside Docker, the API retrieves discovery
metadata over the Compose network from
`http://keycloak:8080/realms/ayoos/.well-known/openid-configuration`, while
still validating the public issuer in the token.

## Tenant resolution

Authentication runs before Finbuckle tenant resolution. If an authenticated
token contains a non-empty `practice` or `tenant` claim whose value is a
registered practice slug, that claim is tried first. The existing `X-Tenant`
header strategy remains the fallback:

```http
X-Tenant: downtown-family-clinic
```

This fallback is needed for initial practice creation and for users whose token
does not yet carry a practice assignment. The realm includes a mapper from the
Keycloak user attribute `practice` to the `practice` token claim so practice
assignments can later be managed without changing API code. Tenant routing does
not replace role authorization.

## Local development login

Start from a clean local environment when testing realm import:

```bash
docker compose down -v
docker compose up -d --build
```

Open `http://localhost:3000`, select **Sign in**, and use:

- Username: `admin@ayoos.local`
- Password: `Dev12345!`
- Realm role: `practice-admin`

The user can open `/setup/practice` and create the first practice. API Swagger
is available at `http://localhost:5000/swagger`; select **Authorize** to use the
same Keycloak login and PKCE flow.

> **Development seed only:** `admin@ayoos.local` and its checked-in password
> exist solely to make a disposable local environment usable. This seed user
> must never be imported, created, or retained in production. Production realm
> configuration must provision administrators through a secure deployment
> process and must not reuse any local credentials.

## Configuration

The root `.env.example` documents Compose values:

- `KEYCLOAK_AUTHORITY`
- `KEYCLOAK_METADATA_ADDRESS`
- `KEYCLOAK_AUDIENCE`
- `KEYCLOAK_FRONTEND_CLIENT_ID`

Compose maps these to the API's `Keycloak__Authority`,
`Keycloak__MetadataAddress`, `Keycloak__Audience`, and
`Keycloak__FrontendClientId` configuration keys and to the frontend's public
build-time OIDC settings.
