# TeamForge Reference Architecture

Multi-tenant SaaS project management platform with row-level security (RLS).

## Tech Stack

### Backend (`src/`)
- **.NET 8** Web API (`TeamForge.Api`)
- **Entity Framework Core** with SQL Server (`TeamForge.Data`)
- **JWT authentication** with tenant-scoped tokens + **MS Entra ID SSO** (token exchange)
- **xUnit** tests (`TeamForge.Tests`) — run with `cd src && dotnet test`

### Frontend (`client/`)
- **Angular 17.3** with standalone components
- **Angular Material** for UI
- **Vitest** + `@analogjs/vitest-angular` for unit tests — run with `cd client && npx vitest run`
- **Playwright** for E2E tests — run with `cd client && npx playwright test`
- **ESLint** via angular-eslint — run with `cd client && npx ng lint`

### Database (`database/`)
- **SQL Server** with RLS policies for tenant isolation

## Key Commands

```bash
# Frontend
cd client && npm start          # Dev server (port 4200)
cd client && npx vitest run     # Unit tests (83 tests)
cd client && npx playwright test # E2E tests

# Backend
cd src && dotnet run --project TeamForge.Api  # API server
cd src && dotnet test                          # Backend tests
```

## Project Conventions

- Angular components use standalone pattern (no NgModules)
- Test files use `.spec.ts` suffix, co-located with source
- Vitest globals enabled — no need to import `describe`, `it`, `expect`
- Use `vi.spyOn()` for spies, `vi.fn()` for mock functions
- Feature pages use `.page.ts` suffix (e.g., `login.page.ts`)
- Environment configs in `client/src/environments/`

## MS Entra ID SSO Setup

To enable Microsoft Entra ID (Azure AD) sign-in:

1. **Azure Portal** → Entra ID → App registrations → New registration
2. Set **Redirect URI**: `http://localhost:4200` (SPA type)
3. **Expose an API** → Add scope `access_as_user`
4. **API permissions** → Grant `User.Read` (delegated)
5. Copy **Client ID** and **Tenant ID** into:
   - `src/TeamForge.Api/appsettings.json` → `AzureAd` section
   - `client/src/environments/environment.ts` → `azure` section
6. The hybrid auth flow: MSAL popup → Entra token → `POST /auth/entra-login` → TeamForge JWT
