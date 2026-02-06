using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TeamForge.Api.Models;
using TeamForge.Api.Services;
using TeamForge.Data;
using TeamForge.Data.Entities;
using Xunit;

namespace TeamForge.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly TeamForgeDbContext _db;
    private readonly AuthService _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<TeamForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TeamForgeDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "SuperSecretKeyForTestingThatIsLongEnough123456",
                ["Jwt:Issuer"] = "TeamForge",
                ["Jwt:Audience"] = "TeamForgeClient"
            })
            .Build();

        var logger = new Mock<ILogger<AuthService>>();
        _sut = new AuthService(_db, config, logger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var tenant = new Tenant { Id = _tenantId, CompanyName = "Test Corp" };
        _db.Tenants.Add(tenant);

        _db.TenantBranding.Add(new TenantBranding { TenantId = _tenantId });

        var adminRole = new Role { TenantId = _tenantId, Name = "Admin" };
        _db.Roles.Add(adminRole);

        var user = new AppUser
        {
            TenantId = _tenantId,
            Email = "test@test.com",
            DisplayName = "Test User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        _db.AppUsers.Add(user);

        _db.UserRoles.Add(new UserRole
        {
            TenantId = _tenantId,
            UserId = user.Id,
            RoleId = adminRole.Id
        });

        _db.SaveChanges();
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@test.com", Password = "password123" };

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("test@test.com", result.Email);
        Assert.Equal("Test User", result.DisplayName);
        Assert.Equal(_tenantId.ToString(), result.TenantId);
        Assert.Equal("Admin", result.Role);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        var request = new LoginRequest { Email = "test@test.com", Password = "wrongpassword" };

        var result = await _sut.LoginAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_NonexistentEmail_ReturnsNull()
    {
        var request = new LoginRequest { Email = "nobody@test.com", Password = "password123" };

        var result = await _sut.LoginAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesTenantAndUser()
    {
        // Arrange
        var request = new RegisterRequest
        {
            CompanyName = "New Corp",
            Email = "admin@newcorp.com",
            DisplayName = "New Admin",
            Password = "securepass123"
        };

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("admin@newcorp.com", result.Email);
        Assert.Equal("New Corp", result.TenantName);
        Assert.Equal("Admin", result.Role);

        // Verify tenant was created
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.CompanyName == "New Corp");
        Assert.NotNull(tenant);

        // Verify branding was created
        var branding = await _db.TenantBranding.FirstOrDefaultAsync(b => b.TenantId == tenant.Id);
        Assert.NotNull(branding);
    }

    [Fact]
    public async Task DemoLoginAsync_ExistingTenant_ReturnsAuthResponse()
    {
        var result = await _sut.DemoLoginAsync("Test Corp");

        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("Test Corp", result.TenantName);
    }

    [Fact]
    public async Task DemoLoginAsync_NonexistentTenant_FallsBackToFirstTenant()
    {
        var result = await _sut.DemoLoginAsync("Nonexistent");

        Assert.NotNull(result);
        Assert.Equal("Test Corp", result.TenantName);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewAuthResponse()
    {
        // Get initial token
        var login = await _sut.LoginAsync(new LoginRequest { Email = "test@test.com", Password = "password123" });
        Assert.NotNull(login);

        // Refresh it
        var result = await _sut.RefreshTokenAsync(login.Token);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("test@test.com", result.Email);
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ReturnsNull()
    {
        var result = await _sut.RefreshTokenAsync("invalid.token.here");

        Assert.Null(result);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
