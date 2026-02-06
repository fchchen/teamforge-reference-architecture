-- TeamForge Row-Level Security Setup
-- This script creates the security schema, predicate function, and security policies
-- for multi-tenant data isolation using SQL Server SESSION_CONTEXT.

-- Step 1: Create security schema for RLS objects
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'security')
    EXEC('CREATE SCHEMA security');
GO

-- Step 2: Create the tenant access predicate function
-- Reads TenantId from SESSION_CONTEXT set by TenantResolutionMiddleware
CREATE OR ALTER FUNCTION security.fn_TenantAccessPredicate(@TenantId UNIQUEIDENTIFIER)
RETURNS TABLE
WITH SCHEMABINDING
AS
    RETURN SELECT 1 AS fn_AccessResult
    WHERE @TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS UNIQUEIDENTIFIER);
GO

-- Step 3: Create security policies for each tenant-scoped table
-- Note: Tenants table itself does NOT have RLS (it's a lookup table)

-- TenantBranding
CREATE SECURITY POLICY security.TenantBrandingPolicy
    ADD FILTER PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.TenantBranding,
    ADD BLOCK PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.TenantBranding
    WITH (STATE = ON);
GO

-- AppUsers
CREATE SECURITY POLICY security.AppUsersPolicy
    ADD FILTER PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.AppUsers,
    ADD BLOCK PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.AppUsers
    WITH (STATE = ON);
GO

-- Roles
CREATE SECURITY POLICY security.RolesPolicy
    ADD FILTER PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.Roles,
    ADD BLOCK PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.Roles
    WITH (STATE = ON);
GO

-- UserRoles
CREATE SECURITY POLICY security.UserRolesPolicy
    ADD FILTER PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.UserRoles,
    ADD BLOCK PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.UserRoles
    WITH (STATE = ON);
GO

-- Projects
CREATE SECURITY POLICY security.ProjectsPolicy
    ADD FILTER PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.Projects,
    ADD BLOCK PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.Projects
    WITH (STATE = ON);
GO

-- Teams
CREATE SECURITY POLICY security.TeamsPolicy
    ADD FILTER PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.Teams,
    ADD BLOCK PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.Teams
    WITH (STATE = ON);
GO

-- TeamMembers
CREATE SECURITY POLICY security.TeamMembersPolicy
    ADD FILTER PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.TeamMembers,
    ADD BLOCK PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.TeamMembers
    WITH (STATE = ON);
GO

-- Announcements
CREATE SECURITY POLICY security.AnnouncementsPolicy
    ADD FILTER PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.Announcements,
    ADD BLOCK PREDICATE security.fn_TenantAccessPredicate(TenantId) ON tenant.Announcements
    WITH (STATE = ON);
GO
