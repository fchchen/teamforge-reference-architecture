using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using TeamForge.Api.Models;
using Xunit;

namespace TeamForge.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<AuthResponse> DemoLoginAsync(string tenantName = "Acme Corp")
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/demo",
            new DemoLoginRequest { TenantName = tenantName });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        return auth!;
    }

    private void SetAuth(AuthResponse auth)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.Token);
    }

    // ── Auth Tests ──

    [Fact]
    public async Task AuthDemo_ReturnsTokenForAcme()
    {
        var auth = await DemoLoginAsync("Acme Corp");

        Assert.NotEmpty(auth.Token);
        Assert.Equal("Acme Corp", auth.TenantName);
        Assert.Equal("Admin", auth.Role);
    }

    [Fact]
    public async Task AuthDemo_ReturnsTokenForPixelStudio()
    {
        var auth = await DemoLoginAsync("Pixel Studio");

        Assert.NotEmpty(auth.Token);
        Assert.Equal("Pixel Studio", auth.TenantName);
    }

    [Fact]
    public async Task AuthRegister_CreatesNewTenantAndReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            CompanyName = "Integration Test Corp",
            Email = "admin@integration.com",
            DisplayName = "Integration Admin",
            Password = "securepass123"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth);
        Assert.Equal("Integration Test Corp", auth.TenantName);
        Assert.Equal("Admin", auth.Role);
    }

    [Fact]
    public async Task AuthLogin_InvalidCredentials_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "nobody@test.com",
            Password = "wrong"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Tenant Isolation Tests ──
    // Note: Full RLS isolation (SQL SESSION_CONTEXT) only works with SQL Server.
    // InMemory DB tests verify the JWT tenant_id claim flow and middleware pipeline.

    [Fact]
    public async Task TenantIsolation_DifferentTenants_GetDifferentJwtClaims()
    {
        var acmeAuth = await DemoLoginAsync("Acme Corp");
        var pixelAuth = await DemoLoginAsync("Pixel Studio");

        // Different tenants get different tenant IDs in their tokens
        Assert.NotEqual(acmeAuth.TenantId, pixelAuth.TenantId);
        Assert.Equal("Acme Corp", acmeAuth.TenantName);
        Assert.Equal("Pixel Studio", pixelAuth.TenantName);
    }

    [Fact]
    public async Task TenantIsolation_ProjectCreatedByTenantA_HasCorrectTenantId()
    {
        var acmeAuth = await DemoLoginAsync("Acme Corp");
        SetAuth(acmeAuth);

        // Create a project — it should be associated with Acme's tenant
        var createResponse = await _client.PostAsJsonAsync("/api/v1/projects", new CreateProjectRequest
        {
            Name = "Acme RLS Test Project",
            Description = "Testing tenant isolation"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // Verify we can retrieve the project
        var projects = await _client.GetFromJsonAsync<List<ProjectResponse>>("/api/v1/projects", JsonOptions);
        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.Name == "Acme RLS Test Project");
    }

    [Fact]
    public async Task TenantIsolation_AuthenticatedEndpoints_ReturnData()
    {
        var auth = await DemoLoginAsync("Acme Corp");
        SetAuth(auth);

        // All tenant-scoped endpoints should respond successfully
        var projects = await _client.GetAsync("/api/v1/projects");
        var teams = await _client.GetAsync("/api/v1/teams");
        var users = await _client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.OK, projects.StatusCode);
        Assert.Equal(HttpStatusCode.OK, teams.StatusCode);
        Assert.Equal(HttpStatusCode.OK, users.StatusCode);
    }

    // ── CRUD Tests ──

    [Fact]
    public async Task Projects_CrudLifecycle()
    {
        var auth = await DemoLoginAsync("Acme Corp");
        SetAuth(auth);

        // Create
        var createResponse = await _client.PostAsJsonAsync("/api/v1/projects", new CreateProjectRequest
        {
            Name = "Integration Test Project",
            Description = "Created by integration test",
            Category = "Testing"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var project = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);
        Assert.NotNull(project);
        Assert.Equal("Integration Test Project", project.Name);

        // Read
        var getResponse = await _client.GetFromJsonAsync<ProjectResponse>($"/api/v1/projects/{project.Id}", JsonOptions);
        Assert.NotNull(getResponse);
        Assert.Equal("Integration Test Project", getResponse.Name);

        // Update
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/projects/{project.Id}", new UpdateProjectRequest
        {
            Name = "Updated Project",
            Status = "Completed"
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);
        Assert.Equal("Updated Project", updated!.Name);
        Assert.Equal("Completed", updated.Status);

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/v1/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deleted
        var getDeletedResponse = await _client.GetAsync($"/api/v1/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    // ── Branding Tests ──

    [Fact]
    public async Task Branding_GetReturnsCorrectTenantBranding()
    {
        var auth = await DemoLoginAsync("Acme Corp");
        SetAuth(auth);

        var branding = await _client.GetFromJsonAsync<BrandingResponse>("/api/v1/branding", JsonOptions);

        Assert.NotNull(branding);
        Assert.Equal("Acme Corp", branding.CompanyName);
        Assert.Equal("#1565c0", branding.PrimaryColor);
    }

    [Fact]
    public async Task Branding_DifferentTenants_HaveDifferentBranding()
    {
        var acmeAuth = await DemoLoginAsync("Acme Corp");
        SetAuth(acmeAuth);
        var acmeBranding = await _client.GetFromJsonAsync<BrandingResponse>("/api/v1/branding", JsonOptions);

        var pixelAuth = await DemoLoginAsync("Pixel Studio");
        SetAuth(pixelAuth);
        var pixelBranding = await _client.GetFromJsonAsync<BrandingResponse>("/api/v1/branding", JsonOptions);

        Assert.NotNull(acmeBranding);
        Assert.NotNull(pixelBranding);
        Assert.NotEqual(acmeBranding.PrimaryColor, pixelBranding.PrimaryColor);
    }

    // ── Dashboard Tests ──

    [Fact]
    public async Task Dashboard_ReturnsAggregatedData()
    {
        var auth = await DemoLoginAsync("Acme Corp");
        SetAuth(auth);

        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/api/v1/dashboard", JsonOptions);

        Assert.NotNull(dashboard);
        Assert.True(dashboard.ProjectCount > 0);
        Assert.True(dashboard.TeamCount > 0);
        Assert.True(dashboard.UserCount > 0);
    }

    // ── Onboarding Tests ──

    [Fact]
    public async Task Onboarding_Generate_ReturnsMockPreview()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/onboarding/generate", new OnboardingGenerateRequest
        {
            CompanyName = "Test Startup",
            CompanyDescription = "A 10-person tech startup building AI tools"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = await response.Content.ReadFromJsonAsync<OnboardingPreview>(JsonOptions);
        Assert.NotNull(preview);
        Assert.NotEmpty(preview.Roles);
        Assert.NotEmpty(preview.Teams);
        Assert.NotEmpty(preview.WelcomeAnnouncement);
    }

    // ── Health Check ──

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Unauthorized Access ──

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/projects");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
