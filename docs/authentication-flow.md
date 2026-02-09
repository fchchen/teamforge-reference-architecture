# TeamForge Authentication & Authorization Flow

## Overview

TeamForge uses a hybrid authentication architecture supporting three login methods, all of which converge on a single TeamForge JWT that drives authorization and tenant isolation via SQL Server Row-Level Security (RLS).

```
                         ┌──────────────────────────────────┐
                         │         Login Methods             │
                         ├──────────┬───────────┬────────────┤
                         │  Demo    │  Email/   │  Entra ID  │
                         │  Login   │  Password │  SSO       │
                         └────┬─────┴─────┬─────┴──────┬─────┘
                              │           │            │
                              │           │     ┌──────┴──────┐
                              │           │     │ MSAL Popup   │
                              │           │     │ ↓ Entra Token │
                              │           │     └──────┬──────┘
                              ▼           ▼            ▼
                         ┌──────────────────────────────────┐
                         │     POST /api/v1/auth/*          │
                         │     (AuthController)             │
                         └────────────────┬─────────────────┘
                                          │
                                          ▼
                         ┌──────────────────────────────────┐
                         │     AuthService                  │
                         │     - Validates credentials      │
                         │     - Generates TeamForge JWT    │
                         │     (8-hour expiry, HMAC-SHA256) │
                         └────────────────┬─────────────────┘
                                          │
                                          ▼
                         ┌──────────────────────────────────┐
                         │     Frontend (AuthService)       │
                         │     - Stores JWT in localStorage │
                         │     - Signal-based auth state    │
                         └────────────────┬─────────────────┘
                                          │
                          (subsequent API requests)
                                          │
                                          ▼
                         ┌──────────────────────────────────┐
                         │     auth.interceptor.ts          │
                         │     Authorization: Bearer <JWT>  │
                         └────────────────┬─────────────────┘
                                          │
                                          ▼
                         ┌──────────────────────────────────┐
                         │     .NET JWT Middleware           │
                         │     (Program.cs — validates JWT) │
                         └────────────────┬─────────────────┘
                                          │
                                          ▼
                         ┌──────────────────────────────────┐
                         │     TenantResolutionMiddleware   │
                         │     - Reads tenant_id claim      │
                         │     - sp_set_session_context      │
                         └────────────────┬─────────────────┘
                                          │
                                          ▼
                         ┌──────────────────────────────────┐
                         │     SQL Server RLS               │
                         │     fn_TenantAccessPredicate     │
                         │     filters rows by TenantId     │
                         └──────────────────────────────────┘
```

---

## Authentication Methods

### 1. Demo Login

The simplest auth path, intended for quick evaluation.

**Flow:** User clicks "Demo Login" on `LoginPage` → `AuthService.demoLogin(tenantName)` → `POST /api/v1/auth/demo`

**Backend (`AuthService.DemoLoginAsync`):** Looks up the tenant by company name (falls back to first active tenant), finds the first active user in that tenant, and returns a TeamForge JWT.

No credentials are required. This is a development/demo convenience.

### 2. Email/Password Login

Standard credential-based authentication.

**Flow:** User submits form on `LoginPage` → `AuthService.login({email, password})` → `POST /api/v1/auth/login`

**Backend (`AuthService.LoginAsync`):** Looks up the user by email (including tenant and role joins), verifies the password with BCrypt (`BCrypt.Net.BCrypt.Verify`), updates `LastLoginAt`, and returns a TeamForge JWT.

### 3. MS Entra ID SSO (Hybrid Token Exchange)

The most complex flow — a hybrid where the SPA acquires an Entra token via MSAL, then exchanges it for a TeamForge JWT on the backend.

**Flow:**

```
1. User clicks "Sign in with Microsoft" on LoginPage
2. LoginPage.signInWithMicrosoft() → AuthService.entraLogin()
3. MsalService.loginPopup() opens Entra ID popup
4. User authenticates with Microsoft
5. MSAL returns Entra access token (scoped to api://<clientId>/access_as_user)
6. POST /api/v1/auth/entra-login { accessToken: "<entra_token>" }
7. Backend validates Entra token against OIDC metadata (signing keys, issuer, audience)
8. Backend looks up user by EntraIdObjectId (oid claim)
   ├─ User found → return { isProvisioned: true, auth: <TeamForge JWT> }
   └─ User not found → return { isProvisioned: false }
9. If not provisioned:
   a. Frontend stores pendingEntraToken, navigates to /entra-provision
   b. User provides company name and display name
   c. POST /api/v1/auth/entra-provision { accessToken, companyName, displayName }
   d. Backend creates tenant + user (no password, linked via EntraIdObjectId)
   e. Returns TeamForge JWT
```

**Why hybrid?** The Entra token authenticates the user's identity, but TeamForge needs its own JWT containing `tenant_id` for RLS. The backend validates the Entra token, maps it to a TeamForge user/tenant, and issues a TeamForge-scoped JWT.

---

## Frontend Auth Architecture

### `MsalService` — `client/src/app/core/services/msal.service.ts`

Wraps `@azure/msal-browser` (`PublicClientApplication`) for Entra ID interaction.

| Method | Purpose |
|---|---|
| `initialize()` | Creates and initializes MSAL instance with client ID, authority, and redirect URI from environment config. Uses `sessionStorage` for MSAL cache. |
| `loginPopup()` | Opens Entra ID popup, requests `access_as_user` scope, returns the access token string. |
| `acquireTokenSilent()` | Silently acquires a fresh Entra token for an already-authenticated account. |
| `logout()` | Opens MSAL logout popup. |

### `AuthService` — `client/src/app/core/services/auth.service.ts`

Central auth state manager using Angular signals.

**State signals:**
- `authResponse` — the full `AuthResponse` object (token, email, displayName, tenantId, tenantName, role, expiration)
- `error` — current error message
- `isLoading` — loading state for UI spinners
- `pendingEntraToken` — holds the Entra access token during provisioning flow

**Computed signals:**
- `isAuthenticated` — checks `authResponse` exists and `expiration > now`
- `currentUser` — `displayName`
- `token` — JWT string (used by interceptor)
- `tenantId`, `tenantName`, `role` — extracted from auth response

**Login methods:**
- `login(request)` — `POST /auth/login` with email/password
- `register(request)` — `POST /auth/register` with company, email, display name, password
- `demoLogin(tenantName)` — `POST /auth/demo`
- `entraLogin()` — MSAL popup → `POST /auth/entra-login` → routes to dashboard or provision page
- `entraProvision(companyName, displayName)` — `POST /auth/entra-provision`

**Storage:** Auth response is persisted to `localStorage` under key `tf_auth`. On construction, `hydrateFromStorage()` restores the session if the token hasn't expired. On logout, the key is removed.

### `authInterceptor` — `client/src/app/core/interceptors/auth.interceptor.ts`

A functional `HttpInterceptorFn` that reads `AuthService.token()` (computed signal) and clones each outgoing request to add `Authorization: Bearer <token>` if a token exists.

```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).token();
  if (token) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  return next(req);
};
```

### `authGuard` / `adminGuard` — `client/src/app/core/guards/auth.guard.ts`

Functional `CanActivateFn` guards:

- **`authGuard`** — redirects to `/login?returnUrl=...` if `isAuthenticated()` is false
- **`adminGuard`** — redirects to `/dashboard` if user is not authenticated or role is not `'Admin'`

---

## Backend Auth Architecture

### `Program.cs` — `src/TeamForge.Api/Program.cs`

Configures the JWT validation and middleware pipeline.

**JWT setup (lines 55-82):**
- In development, auto-generates a random 64-byte key if `Jwt:Key` is not configured
- In production, requires `Jwt:Key` to be explicitly set
- Validation: issuer (`TeamForge`), audience (`TeamForgeClient`), signing key (HMAC-SHA256), lifetime with 1-minute clock skew

**Middleware pipeline order (lines 117-139):**
```
1. GlobalExceptionHandling
2. RequestLogging
3. Swagger (dev only)
4. CORS (localhost:4200)
5. Routing
6. Authentication          ← validates JWT
7. Authorization
8. TenantResolution        ← extracts tenant_id, sets SESSION_CONTEXT
9. MapControllers
```

The order is critical: `TenantResolution` must run after `Authentication` (needs the validated JWT claims) and before controllers (must set `SESSION_CONTEXT` before any DB queries).

### `AuthController` — `src/TeamForge.Api/Controllers/AuthController.cs`

Route: `api/v1/auth`

| Endpoint | Method | Purpose |
|---|---|---|
| `/auth/login` | POST | Email/password login → `AuthResponse` or 401 |
| `/auth/register` | POST | New tenant + admin user registration → `AuthResponse` or 400 |
| `/auth/demo` | POST | Demo login (optional tenant name) → `AuthResponse` or 404 |
| `/auth/refresh` | POST | Exchange existing JWT for a fresh one → `AuthResponse` or 401 |
| `/auth/entra-login` | POST | Exchange Entra access token → `EntraLoginResponse` or 401 |
| `/auth/entra-provision` | POST | Create tenant/user from Entra token → `AuthResponse` or 401/400 |

### `AuthService` — `src/TeamForge.Api/Services/AuthService.cs`

Core authentication logic.

**JWT generation (`GenerateJwt`, line 180):**
- Algorithm: HMAC-SHA256
- Issuer: `Jwt:Issuer` config (default: `"TeamForge"`)
- Audience: `Jwt:Audience` config (default: `"TeamForgeClient"`)
- Expiration: 8 hours from generation
- Claims: see [JWT Claims](#jwt-claims) below

**Password hashing:** Uses `BCrypt.Net.BCrypt` for hashing (`HashPassword`) and verification (`Verify`).

**Entra token validation (`ValidateEntraTokenAsync`, line 318):**
1. Fetches OIDC metadata from `https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration`
2. Validates the Entra access token against:
   - Signing keys from OIDC metadata
   - Issuers: both v1 (`sts.windows.net/{tenantId}/`) and v2 (`login.microsoftonline.com/{tenantId}/v2.0`)
   - Audiences: both `api://{clientId}` and `{clientId}`
   - Lifetime (must not be expired)
3. Extracts `oid` (object identifier), `preferred_username`/`email`, and `name` claims

### `TenantResolutionMiddleware` — `src/TeamForge.Api/Middleware/TenantResolutionMiddleware.cs`

Runs on every authenticated request. Bridges JWT claims to SQL Server `SESSION_CONTEXT` for RLS.

```csharp
// Extracts tenant_id from JWT claims
var tenantClaim = context.User.FindFirst("tenant_id")?.Value;

if (Guid.TryParse(tenantClaim, out var tenantId))
{
    // Sets SESSION_CONTEXT for SQL RLS filter predicates
    await db.SetTenantContextAsync(tenantId, context.RequestAborted);
    context.Items["TenantId"] = tenantId;
}
```

Also stores `TenantId` in `HttpContext.Items` for controller access.

---

## JWT Claims

The TeamForge JWT contains these claims:

| Claim | JWT Key | Example | Source |
|---|---|---|---|
| User ID | `nameid` (`ClaimTypes.NameIdentifier`) | `a1b2c3d4-...` | `AppUser.Id` |
| Tenant ID | `tenant_id` (custom) | `e5f6a7b8-...` | `AppUser.TenantId` |
| Email | `email` (`ClaimTypes.Email`) | `user@example.com` | `AppUser.Email` |
| Role | `role` (`ClaimTypes.Role`) | `Admin` or `Member` | `Role.Name` |
| Issuer | `iss` | `TeamForge` | `Jwt:Issuer` config |
| Audience | `aud` | `TeamForgeClient` | `Jwt:Audience` config |
| Expiration | `exp` | Unix timestamp | 8 hours from issue |

---

## Token Lifecycle

### 1. Creation
All three login methods end in `AuthService.GenerateAuthResponse()`, which calls `GenerateJwt()` to create a signed JWT with an 8-hour expiry.

### 2. Transmission to Client
The JWT is returned as part of an `AuthResponse` JSON object along with metadata (email, displayName, tenantId, tenantName, role, expiration).

### 3. Client Storage
`AuthService.setAuth()` stores the entire `AuthResponse` in:
- An Angular signal (`authResponse`) for reactive UI updates
- `localStorage` under key `tf_auth` for session persistence across page reloads

### 4. Session Hydration
On app startup, `AuthService` constructor calls `hydrateFromStorage()`, which reads `tf_auth` from `localStorage`. If the token hasn't expired, it restores the signal state. If expired, it clears the storage.

### 5. Request Transmission
`authInterceptor` reads `AuthService.token()` (a computed signal deriving the JWT string from `authResponse`) and attaches it as `Authorization: Bearer <token>` on every outgoing HTTP request.

### 6. Server Validation
The .NET `JwtBearerDefaults` middleware validates the token's signature, issuer, audience, and lifetime on every request. Invalid/expired tokens receive a 401 response.

### 7. Expiration
Tokens expire 8 hours after issuance. The `/auth/refresh` endpoint allows exchanging a valid (or expired) token for a fresh one — lifetime validation is disabled for refresh to allow seamless re-authentication.

---

## Row-Level Security (RLS)

RLS ensures tenant data isolation at the database level, making it impossible for application bugs to leak data across tenants.

### How `tenant_id` Flows from JWT to RLS

```
JWT claim "tenant_id"
        │
        ▼
TenantResolutionMiddleware
        │ reads claim from HttpContext.User
        ▼
TeamForgeDbContext.SetTenantContextAsync(tenantId)
        │ EXEC sp_set_session_context @key = N'TenantId', @value = <tenantId>
        ▼
SQL Server SESSION_CONTEXT('TenantId')
        │
        ▼
security.fn_TenantAccessPredicate(@TenantId)
        │ WHERE @TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS UNIQUEIDENTIFIER)
        ▼
Security Policies (FILTER + BLOCK predicates)
        │ applied to: AppUsers, Roles, UserRoles, Projects,
        │             Teams, TeamMembers, Announcements, TenantBranding
        ▼
Only rows matching the authenticated tenant are visible/writable
```

### RLS Components (`database/001_rls_setup.sql`)

1. **Predicate function:** `security.fn_TenantAccessPredicate(@TenantId)` — returns 1 if the row's `TenantId` matches `SESSION_CONTEXT('TenantId')`
2. **Filter predicates:** Applied to SELECT queries — rows from other tenants are invisible
3. **Block predicates:** Applied to INSERT/UPDATE/DELETE — prevents writing rows for other tenants
4. **Excluded table:** `Tenants` itself has no RLS (it's a lookup table needed during authentication before tenant context is set)

---

## Debugging Guide

### Chrome DevTools

**Network tab:**
- Filter by `auth` to see login/register/entra-login requests
- Check request payload for credentials being sent
- Check response body for the `AuthResponse` (token, tenantId, role, expiration)
- On subsequent API calls, check the `Authorization` header — should be `Bearer <jwt>`

**Application tab → Local Storage → `http://localhost:4200`:**
- Look for key `tf_auth` — contains the full `AuthResponse` JSON
- You can decode the `token` field at jwt.io to inspect claims (nameid, tenant_id, email, role, exp)

**Sources tab (breakpoints):**
- `auth.service.ts` → `setAuth()` — when auth state changes
- `auth.service.ts` → `hydrateFromStorage()` — on page load/refresh
- `auth.interceptor.ts` → line 7 (`const token = ...`) — to verify token attachment
- `login.page.ts` → `signInWithMicrosoft()` — Entra flow start

### .NET Debugger (Visual Studio / VS Code / Rider)

**Key breakpoint locations:**
- `AuthController.cs:26` — `Login` action entry
- `AuthController.cs:78` — `EntraLogin` action entry
- `AuthService.cs:162` — `GenerateAuthResponse` — inspect generated JWT claims
- `AuthService.cs:318` — `ValidateEntraTokenAsync` — Entra token validation
- `TenantResolutionMiddleware.cs:25` — `tenant_id` claim extraction
- `TenantResolutionMiddleware.cs:30` — `SetTenantContextAsync` call

### Common Issues

| Symptom | Likely Cause | Check |
|---|---|---|
| 401 on all API calls | JWT expired or missing | Check `tf_auth` in localStorage; check `Authorization` header in Network tab |
| Empty data on dashboard | `SESSION_CONTEXT` not set | Breakpoint in `TenantResolutionMiddleware`; verify `tenant_id` claim exists in JWT |
| Entra login fails with "Invalid Entra ID token" | Token audience/issuer mismatch | Verify `AzureAd:ClientId` and `AzureAd:TenantId` in `appsettings.json` match the Azure app registration |
| MSAL popup blocked | Browser popup blocker | Allow popups for `localhost:4200` |
| "MSAL not initialized" error | `MsalService.initialize()` not called | Ensure MSAL is initialized before `loginPopup()` |

---

## Why Shared Database + RLS? Benefits & Industry Adoption

### Benefits of This Pattern

**1. Defense-in-Depth**

Without RLS, a single missing `.Where()` filter leaks all tenants' data:

```csharp
// Bug: forgot tenant filter — returns EVERY tenant's projects
var projects = await _db.Projects.ToListAsync();
```

With RLS, that same bug is harmless — SQL Server silently filters to only the current tenant's rows. The database protects against application-level mistakes.

**2. Cannot Be Bypassed by Application Code**

RLS operates at the SQL engine level. Even raw SQL queries, stored procedures, or code written by a developer unaware of the tenant rules — all get filtered. There is no way for application code to accidentally read or write another tenant's data.

**3. Simpler Queries**

Without RLS, every query needs `.Where(x => x.TenantId == tenantId)`. With RLS, the database handles tenant filtering invisibly — queries can be written without explicitly thinking about isolation.

**4. Single Database, Lower Cost**

Alternative approaches require separate databases or schemas per tenant. RLS enables a single shared database for all tenants while still guaranteeing isolation, which is significantly cheaper to operate and maintain.

### Multi-Tenant Architecture Patterns Comparison

| Pattern | Example Users | Tradeoff |
|---|---|---|
| **Database-per-tenant** | Salesforce (large orgs) | Strongest isolation, highest cost and operational complexity |
| **Schema-per-tenant** | Some legacy ERPs | Moderate isolation, complex migrations |
| **Shared DB + RLS** (this repo) | Azure SaaS templates, Supabase, PostHog, Citus | Cost-effective, single schema, requires careful setup |

### Industry Adoption

- **Microsoft** — their official [Azure SaaS multi-tenant guidance](https://learn.microsoft.com/en-us/azure/azure-sql/database/saas-tenancy-app-design-patterns) recommends exactly this pattern with `SESSION_CONTEXT` + RLS for SQL Server
- **Supabase** — built their entire multi-tenant auth model on PostgreSQL RLS
- **PostgreSQL ecosystem** — Postgres RLS is widely adopted (same concept, different SQL flavor)
- **Citus / Azure Cosmos DB for PostgreSQL** — RLS as a first-class multi-tenancy pattern

### When This Pattern Is NOT the Right Choice

- **Regulatory/compliance requirements** — some industries (healthcare, finance) mandate physical data separation, where RLS alone may not satisfy auditors
- **Very large tenants with different performance needs** — noisy neighbor problem; one tenant's heavy queries can affect all others sharing the database
- **Tenant-specific schema customizations** — RLS assumes all tenants share the same schema; if tenants need custom columns or tables, database-per-tenant is more appropriate

### How TenantId Flows into Every Row

TeamForge uses explicit TenantId assignment at the application layer combined with RLS enforcement at the database layer:

1. **Controllers** extract `tenant_id` from JWT claims via a `GetTenantId()` helper
2. **Every entity creation** explicitly sets `TenantId` on the new object:

```csharp
// Example from ProjectsController.cs
var tenantId = GetTenantId();  // extracted from JWT claims
var project = new Project
{
    TenantId = tenantId,       // explicitly set on every INSERT
    Name = request.Name,
    Description = request.Description,
    Category = request.Category
};
```

3. **RLS BLOCK predicates** act as a safety net — if application code ever attempts to insert a row with a TenantId that doesn't match the current `SESSION_CONTEXT`, SQL Server rejects the write

There is no `SaveChangesAsync` override, no SQL DEFAULT constraint, no trigger, and no EF Core shadow property that auto-populates TenantId. The explicit-assignment approach ensures clarity and avoids subtle bugs where the wrong tenant context could silently propagate.

---

## Source File Reference

| File | Role |
|---|---|
| `client/src/app/core/services/msal.service.ts` | MSAL wrapper for Entra ID |
| `client/src/app/core/services/auth.service.ts` | Frontend auth state management |
| `client/src/app/core/interceptors/auth.interceptor.ts` | Attaches Bearer token to requests |
| `client/src/app/core/guards/auth.guard.ts` | Route protection (authGuard, adminGuard) |
| `client/src/app/features/login/login.page.ts` | Login page UI component |
| `client/src/environments/environment.ts` | Azure AD client config |
| `src/TeamForge.Api/Controllers/AuthController.cs` | Auth API endpoints |
| `src/TeamForge.Api/Services/AuthService.cs` | JWT generation, Entra validation, BCrypt |
| `src/TeamForge.Api/Program.cs` | JWT middleware config, pipeline order |
| `src/TeamForge.Api/Middleware/TenantResolutionMiddleware.cs` | JWT → SESSION_CONTEXT bridge |
| `src/TeamForge.Data/TeamForgeDbContext.cs` | `SetTenantContextAsync()` — executes `sp_set_session_context` |
| `database/001_rls_setup.sql` | RLS predicate function and security policies |
