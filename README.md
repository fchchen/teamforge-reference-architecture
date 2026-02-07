# TeamForge Reference Architecture

Multi-tenant SaaS project management platform with row-level security. .NET 8 API, Angular 17, SQL Server RLS, MS Entra ID SSO.

## Features

- **Multi-tenant isolation** with SQL Server row-level security (RLS) policies
- **Three auth methods**: email/password registration, one-click demo login, and MS Entra ID SSO
- **Hybrid token exchange**: MSAL popup acquires an Entra ID token, exchanges it for a TeamForge JWT via `POST /auth/entra-login`
- **AI-powered onboarding**: optional Claude API integration for guided setup (falls back to mock when unconfigured)
- **Dynamic white-labeling** per tenant
- **Role-based access control** with Admin and Member roles

## Tech Stack

| Layer | Technologies |
|-------|-------------|
| **Backend** | .NET 8, Entity Framework Core, SQL Server, JWT Bearer Auth, BCrypt, Swagger/OpenAPI |
| **Frontend** | Angular 17, Angular Material, MSAL Browser v5 |
| **Auth** | MS Entra ID (Azure AD), Microsoft.Identity.Web, MSAL Angular v5 |
| **Backend Tests** | xUnit 2.4, Moq, FluentAssertions |
| **Frontend Tests** | Vitest 2.1, @analogjs/vitest-angular |
| **E2E Tests** | Playwright |

## Project Structure

```
teamforge-reference-architecture/
├── src/
│   ├── TeamForge.Api/        # .NET 8 Web API
│   ├── TeamForge.Data/       # EF Core data layer
│   └── TeamForge.Tests/      # xUnit integration + unit tests
├── client/                   # Angular 17 SPA
│   ├── src/
│   │   ├── app/              # Standalone components, services, guards
│   │   └── environments/     # Dev and prod configs
│   └── e2e/                  # Playwright E2E tests
├── database/
│   └── 001_rls_setup.sql     # SQL Server RLS policies
├── docs/                     # Architecture documentation
└── TeamForge.sln             # .NET solution file
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- SQL Server (optional -- falls back to EF Core InMemory database when no connection string is configured)

### Quick Start

Start the backend API:

```bash
cd src
dotnet run --project TeamForge.Api
```

In a separate terminal, start the frontend:

```bash
cd client
npm install
npm start
```

Open `http://localhost:4200` and click **Demo Login** to explore the app with seeded sample data.

## Entra ID SSO Setup

To enable Microsoft Entra ID (Azure AD) single sign-on:

1. Register an app in **Azure Portal** > Entra ID > App registrations
2. Set the **Redirect URI** to `http://localhost:4200` (SPA type)
3. **Expose an API** and add the scope `access_as_user`
4. Grant the `User.Read` delegated permission
5. Copy the **Client ID** and **Tenant ID** into:
   - `src/TeamForge.Api/appsettings.json` (`AzureAd` section)
   - `client/src/environments/environment.ts` (`azure` section)

See [Authentication Flow](docs/authentication-flow.md) for the full hybrid auth flow details.

## Running Tests

**Backend** (xUnit):

```bash
cd src
dotnet test
```

**Frontend unit tests** (Vitest):

```bash
cd client
npx vitest run
```

**E2E tests** (Playwright):

```bash
cd client
npx playwright test
```

## Architecture Overview

TeamForge uses a hybrid authentication flow supporting email/password, demo login, and MS Entra ID SSO. All three methods produce a TeamForge JWT containing user identity and tenant claims. The API passes these claims to SQL Server, where row-level security policies enforce tenant isolation at the database level -- ensuring queries only return data belonging to the authenticated user's tenant.

For a detailed walkthrough, see [Authentication Flow](docs/authentication-flow.md).

## Documentation

- [Authentication & Authorization Flow](docs/authentication-flow.md) -- hybrid auth design, JWT structure, and RLS integration
- [Enterprise Auth Best Practices](docs/enterprise-auth-practices.md) -- architectural guidance for secure multi-tenant SaaS applications

## License

This project does not currently include a license. All rights reserved.
