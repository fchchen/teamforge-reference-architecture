# Enterprise Authentication & Authorization Best Practices

General guidance for building secure, multi-tenant SaaS applications. Not specific to TeamForge — use this as an architectural reference when evaluating patterns and tradeoffs.

---

## Table of Contents

1. [Authentication Patterns](#authentication-patterns)
2. [OAuth 2.0 / OIDC Flows](#oauth-20--oidc-flows)
3. [Token Management](#token-management)
4. [Authorization Patterns](#authorization-patterns)
5. [Multi-Tenancy Isolation](#multi-tenancy-isolation)
6. [Security Best Practices](#security-best-practices)
7. [Zero Trust Principles](#zero-trust-principles)

---

## Authentication Patterns

### Direct IdP Token

The application accepts and uses the identity provider's token directly (e.g., a Google ID token or Entra access token) for all API calls.

**Pros:** Simple, no token exchange layer, fewer moving parts.

**Cons:** Your API is tightly coupled to the IdP's token format. Difficult to add custom claims (tenant, roles). Every API request must validate against the IdP's signing keys (network call to fetch OIDC metadata).

**Best for:** Single-tenant apps where the IdP already provides all the claims you need.

### Token Exchange (Hybrid)

The frontend authenticates with the IdP, then sends the IdP token to your backend, which validates it and issues your own application-scoped token.

**Pros:** Decouples your API from the IdP. You control the token format, claims, and lifetime. Custom claims like `tenant_id` are straightforward. Subsequent API calls validate against your own signing key (fast, no network calls).

**Cons:** Extra endpoint to build and maintain. Two token types to reason about.

**Best for:** Multi-tenant SaaS where you need custom claims (tenant, roles, permissions). This is the pattern TeamForge uses.

### Federated / Brokered Authentication

A central identity broker (e.g., Auth0, Keycloak, Azure AD B2C) sits between your app and multiple IdPs. The broker normalizes tokens from various sources.

**Pros:** Supports many IdPs with a single integration point. Handles protocol translation.

**Cons:** Additional infrastructure to manage. Vendor lock-in risk. Cost at scale.

**Best for:** Applications that need to support many identity providers (Google, Microsoft, SAML enterprise IdPs).

---

## OAuth 2.0 / OIDC Flows

### Authorization Code + PKCE (SPAs and mobile apps)

The recommended flow for public clients (no client secret). PKCE (Proof Key for Code Exchange) prevents authorization code interception attacks.

```
1. App generates code_verifier (random string) and code_challenge (SHA-256 hash)
2. App redirects to IdP /authorize with code_challenge
3. User authenticates, IdP redirects back with authorization code
4. App exchanges code + code_verifier for tokens at /token endpoint
5. IdP verifies code_challenge matches, returns access + ID tokens
```

**Use when:** Your client is a browser SPA, mobile app, or desktop app — any public client that cannot securely store a client secret.

### Client Credentials (Service-to-Service)

For server-to-server communication where no user is involved. The client authenticates with its own credentials (client ID + secret) and receives an access token.

```
1. Service sends client_id + client_secret to IdP /token endpoint
2. IdP returns access token (no refresh token, no user context)
```

**Use when:** Background jobs, microservice-to-microservice calls, daemon processes.

### On-Behalf-Of (OBO)

A middle-tier service exchanges a user's access token for a new token to call a downstream service, preserving the user's identity.

```
1. Frontend sends user's access token to middle-tier API
2. Middle-tier sends token to IdP /token with grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer
3. IdP returns a new access token scoped for the downstream service
4. Middle-tier calls downstream service with new token
```

**Use when:** A backend service needs to call another API on behalf of the authenticated user (e.g., calling Microsoft Graph from your API using the user's delegated permissions).

### Implicit Flow (Deprecated)

Tokens are returned directly in the URL fragment. Vulnerable to token leakage via browser history and referrer headers. **Do not use** — use Authorization Code + PKCE instead.

---

## Token Management

### Access Tokens vs. Refresh Tokens

| | Access Token | Refresh Token |
|---|---|---|
| **Purpose** | Authorize API requests | Obtain new access tokens |
| **Lifetime** | Short (minutes to hours) | Long (days to weeks) |
| **Audience** | Resource server (your API) | Authorization server (IdP) |
| **Sent to** | Every API call | Only the token endpoint |
| **Revocation** | Typically not revocable (stateless) | Revocable (stateful) |

### Token Storage Strategies

| Strategy | XSS Risk | CSRF Risk | Persistence | Best For |
|---|---|---|---|---|
| **In-memory (JS variable)** | Protected (not in DOM) | N/A | Lost on refresh | Highest security SPAs |
| **localStorage** | Vulnerable (accessible to any JS) | N/A | Persists across tabs/refreshes | Convenience-first SPAs |
| **sessionStorage** | Vulnerable (accessible to any JS) | N/A | Lost on tab close | Per-tab isolation |
| **httpOnly cookie** | Protected (not accessible to JS) | Vulnerable (auto-sent) | Configurable | Server-rendered apps |
| **httpOnly cookie + CSRF token** | Protected | Protected | Configurable | Best overall security |

**Recommendation:** For SPAs, if you use `localStorage`, accept the XSS tradeoff and focus on preventing XSS (strict CSP, no inline scripts, sanitize inputs). For highest security, use `httpOnly` cookies with `SameSite=Strict` and a CSRF token.

### Token Rotation

- **Access tokens:** Issue short-lived tokens (15 min to 1 hour). Shorter lifetimes limit the window of abuse for stolen tokens.
- **Refresh tokens:** Implement rotation — each use of a refresh token issues a new refresh token and invalidates the old one. Detect reuse of invalidated refresh tokens as a compromise signal.
- **Sliding expiration:** Reset token expiry on each use for active sessions. Fixed expiration for max session duration.

### Token Expiration Strategies

| Strategy | Tradeoff |
|---|---|
| **Short-lived + refresh** | Best security, more complex client logic |
| **Long-lived (hours)** | Simpler client, larger window if token is stolen |
| **Sliding window** | Good UX for active users, needs server-side tracking |
| **Absolute + sliding combo** | Max 24h absolute, 1h sliding — balances security and UX |

---

## Authorization Patterns

### Role-Based Access Control (RBAC)

Users are assigned roles, and roles have permissions. The simplest and most common model.

```
User → Role → Permissions
alice → Admin → [read, write, delete, manage-users]
bob   → Member → [read, write]
```

**Pros:** Simple to implement and reason about. Works well for most applications.

**Cons:** Role explosion in complex systems. Coarse-grained — hard to express "can edit only their own projects."

### Attribute-Based Access Control (ABAC)

Authorization decisions are based on attributes of the user, resource, action, and environment.

```
ALLOW IF:
  user.department == resource.department
  AND action == "read"
  AND environment.time BETWEEN 09:00 AND 17:00
```

**Pros:** Fine-grained, flexible, context-aware.

**Cons:** Complex to implement, hard to audit, policy language required.

### Policy-Based Access Control

Centralized policy engine (e.g., OPA/Rego, Cedar, Casbin) evaluates authorization decisions.

**Pros:** Decouples policy from code. Policies are auditable and version-controlled.

**Cons:** Additional infrastructure. Learning curve for policy languages.

### Claims-Based Authorization

Authorization decisions are made based on claims in the user's token. The application trusts the token issuer to have validated the claims.

```csharp
// .NET example
[Authorize(Roles = "Admin")]
public IActionResult ManageUsers() { ... }

// Or policy-based
services.AddAuthorization(options =>
{
    options.AddPolicy("TenantAdmin", policy =>
        policy.RequireClaim("role", "Admin")
              .RequireClaim("tenant_id"));
});
```

**Pros:** Stateless — no database lookup needed for authorization. Works naturally with JWT.

**Cons:** Claims are fixed at token issuance. Changes require re-authentication.

---

## Multi-Tenancy Isolation

### Shared Database with Row-Level Security (RLS)

All tenants share a single database. SQL-level predicates filter rows by tenant ID.

```sql
-- Filter predicate: invisible rows from other tenants
CREATE SECURITY POLICY TenantPolicy
    ADD FILTER PREDICATE fn_TenantAccess(TenantId) ON dbo.Projects
    WITH (STATE = ON);
```

**Pros:** Simple infrastructure, easy to manage, cost-effective, works with existing ORMs.

**Cons:** Noisy neighbor risk (one tenant's heavy queries affect others). Single point of failure. Must be careful to set `SESSION_CONTEXT` on every connection.

**Best for:** Most SaaS applications — start here and migrate to more complex patterns only when needed.

### Schema-per-Tenant

Each tenant gets their own database schema within a shared database.

**Pros:** Better isolation than RLS. Easy per-tenant backup and restore. Can customize schema per tenant.

**Cons:** Schema migration complexity multiplied by tenant count. Connection management overhead.

**Best for:** Applications where tenants need slightly different data models or where per-tenant backup/restore is a requirement.

### Database-per-Tenant

Each tenant gets a completely separate database instance.

**Pros:** Strongest isolation. Independent scaling, backup, and disaster recovery. Easiest regulatory compliance (data residency).

**Cons:** Expensive at scale. Complex connection routing. Schema migrations across all databases. Cross-tenant queries are difficult.

**Best for:** Enterprise customers with strict data isolation requirements, regulated industries, or when tenants need independent scaling.

### Comparison Matrix

| Factor | RLS | Schema-per-Tenant | Database-per-Tenant |
|---|---|---|---|
| Isolation strength | Moderate | Good | Strongest |
| Infrastructure cost | Lowest | Medium | Highest |
| Migration complexity | Lowest | High | Highest |
| Per-tenant customization | None | Some | Full |
| Noisy neighbor risk | Highest | Medium | None |
| Operational complexity | Lowest | Medium | Highest |

---

## Security Best Practices

### HTTPS Everywhere

- Enforce TLS 1.2+ on all endpoints
- Use HSTS headers (`Strict-Transport-Security: max-age=31536000; includeSubDomains`)
- Redirect HTTP to HTTPS at the infrastructure level (load balancer/reverse proxy)
- Pin certificates in mobile apps

### CORS Configuration

- **Never** use `Access-Control-Allow-Origin: *` with credentials
- Whitelist specific origins: only your frontend domains
- Restrict allowed methods and headers to what's actually needed
- Set `Access-Control-Max-Age` to reduce preflight requests

```csharp
// Good — specific origin
app.UseCors(policy => policy
    .WithOrigins("https://app.example.com")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());

// Bad — wildcard with credentials (browsers will reject this)
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowCredentials());
```

### CSRF Protection

- For cookie-based auth: use anti-forgery tokens (Synchronizer Token Pattern) or `SameSite=Strict` cookies
- For Bearer token auth (localStorage): CSRF is not a concern because the token is not auto-sent by the browser — the JavaScript must explicitly attach it

### Token Validation Checklist

Every JWT validation should verify:

- [ ] **Signature** — token was signed by a trusted key
- [ ] **Issuer (`iss`)** — token was issued by your auth server
- [ ] **Audience (`aud`)** — token was intended for your API
- [ ] **Expiration (`exp`)** — token has not expired
- [ ] **Not Before (`nbf`)** — token is active (if used)
- [ ] **Algorithm** — reject `alg: none` and unexpected algorithms

### Secret Management

- **Never** commit secrets (JWT keys, connection strings, API keys) to source control
- Use a secret manager in production: Azure Key Vault, AWS Secrets Manager, HashiCorp Vault
- Rotate secrets on a regular schedule and on team member departure
- Use managed identities where possible (no secrets to manage)
- In development, use user-secrets or environment variables — not `appsettings.json` for real credentials

### Rate Limiting on Auth Endpoints

Auth endpoints are prime targets for brute-force attacks. Implement rate limiting:

- **Login:** 5-10 attempts per email per minute, then lock out for 15 minutes
- **Registration:** 3-5 accounts per IP per hour
- **Token refresh:** 10 per user per minute
- **Password reset:** 3 per email per hour

Use middleware (e.g., ASP.NET `RateLimiter`, nginx `limit_req`) or an API gateway.

### Additional Hardening

- **Log all authentication events** — successful logins, failed attempts, token refreshes, logouts
- **Implement account lockout** — temporary lockout after N failed attempts
- **Require strong passwords** — minimum length, complexity requirements, or check against breached password lists (Have I Been Pwned API)
- **Multi-factor authentication (MFA)** — TOTP, WebAuthn/passkeys, or push notifications for sensitive operations

---

## Zero Trust Principles

Zero Trust assumes no implicit trust based on network location or prior authentication. Every request is verified independently.

### 1. Verify Explicitly

- Authenticate and authorize every request based on all available data points: identity, location, device health, service or workload, data classification, anomalies
- Don't rely on network perimeter (VPN, firewall) as a trust boundary
- Validate tokens on every API call — don't cache authorization decisions beyond the token lifetime

### 2. Least Privilege Access

- Grant the minimum permissions needed for the current task
- Use just-in-time (JIT) access for elevated privileges — grant Admin for 1 hour, not permanently
- Scope tokens narrowly: per-resource, per-action where possible
- Prefer short-lived tokens with refresh over long-lived tokens

### 3. Assume Breach

Design every layer as if the layers around it have already been compromised:

- **Defense in depth:** RLS at the database level (not just API-level tenant checks)
- **Encrypt data at rest and in transit** — even within the internal network
- **Segment access:** Microservice A should not have blanket access to Microservice B's database
- **Monitor and alert:** Detect anomalous patterns (impossible travel, unusual data volumes, off-hours access)
- **Incident response plan:** Have a runbook for token compromise, credential leak, and data breach scenarios

### Applying Zero Trust to Multi-Tenant SaaS

| Layer | Zero Trust Control |
|---|---|
| **Frontend** | Token in every request, route guards, expiration checks |
| **API Gateway** | Rate limiting, JWT validation, CORS |
| **Application** | Claims-based authorization, tenant context validation |
| **Database** | RLS filter + block predicates, parameterized queries |
| **Infrastructure** | Network segmentation, managed identities, encrypted connections |

The goal is that a compromise at any single layer cannot lead to cross-tenant data access. Even if an attacker bypasses the API and connects directly to the database, RLS prevents access to other tenants' data (assuming `SESSION_CONTEXT` is not set or set to the wrong tenant).
