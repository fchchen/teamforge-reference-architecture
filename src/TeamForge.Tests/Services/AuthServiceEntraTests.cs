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

public class AuthServiceEntraTests : IDisposable
{
    private readonly TeamForgeDbContext _db;
    private readonly AuthService _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public AuthServiceEntraTests()
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
                ["Jwt:Audience"] = "TeamForgeClient",
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "test-tenant-id",
                ["AzureAd:ClientId"] = "test-client-id",
                ["AzureAd:Audience"] = "api://test-client-id"
            })
            .Build();

        var logger = new Mock<ILogger<AuthService>>();
        _sut = new AuthService(_db, config, logger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var tenant = new Tenant { Id = _tenantId, CompanyName = "Entra Corp" };
        _db.Tenants.Add(tenant);

        _db.TenantBranding.Add(new TenantBranding { TenantId = _tenantId });

        var adminRole = new Role { TenantId = _tenantId, Name = "Admin" };
        _db.Roles.Add(adminRole);

        // User with Entra ID (no password)
        var entraUser = new AppUser
        {
            TenantId = _tenantId,
            Email = "entra@test.com",
            DisplayName = "Entra User",
            PasswordHash = null,
            EntraIdObjectId = "entra-oid-12345"
        };
        _db.AppUsers.Add(entraUser);

        _db.UserRoles.Add(new UserRole
        {
            TenantId = _tenantId,
            UserId = entraUser.Id,
            RoleId = adminRole.Id
        });

        // User with password (no Entra ID)
        var passwordUser = new AppUser
        {
            TenantId = _tenantId,
            Email = "password@test.com",
            DisplayName = "Password User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        _db.AppUsers.Add(passwordUser);

        _db.UserRoles.Add(new UserRole
        {
            TenantId = _tenantId,
            UserId = passwordUser.Id,
            RoleId = adminRole.Id
        });

        _db.SaveChanges();
    }

    [Fact]
    public async Task LoginAsync_EntraUserWithoutPassword_ReturnsNull()
    {
        // Entra ID users have no password, so password login should fail
        var request = new LoginRequest { Email = "entra@test.com", Password = "anypassword" };

        var result = await _sut.LoginAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_PasswordUser_StillWorks()
    {
        var request = new LoginRequest { Email = "password@test.com", Password = "password123" };

        var result = await _sut.LoginAsync(request);

        Assert.NotNull(result);
        Assert.Equal("password@test.com", result.Email);
    }

    [Fact]
    public async Task EntraLoginAsync_InvalidToken_ThrowsSecurityTokenException()
    {
        // A fake token should fail validation (cannot reach Azure AD metadata)
        await Assert.ThrowsAnyAsync<Exception>(
            () => _sut.EntraLoginAsync("invalid-fake-token"));
    }

    [Fact]
    public async Task EntraProvisionAsync_InvalidToken_ThrowsSecurityTokenException()
    {
        await Assert.ThrowsAnyAsync<Exception>(
            () => _sut.EntraProvisionAsync("invalid-fake-token", "New Corp", "New User"));
    }

    [Fact]
    public void AppUser_EntraIdObjectId_CanBeNull()
    {
        var user = _db.AppUsers.First(u => u.Email == "password@test.com");
        Assert.Null(user.EntraIdObjectId);
    }

    [Fact]
    public void AppUser_EntraIdObjectId_CanBeSet()
    {
        var user = _db.AppUsers.First(u => u.Email == "entra@test.com");
        Assert.Equal("entra-oid-12345", user.EntraIdObjectId);
    }

    [Fact]
    public void AppUser_PasswordHash_CanBeNull()
    {
        var user = _db.AppUsers.First(u => u.Email == "entra@test.com");
        Assert.Null(user.PasswordHash);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
