using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TeamForge.Api.Models;
using TeamForge.Api.Services;
using TeamForge.Data;
using TeamForge.Data.Entities;
using Xunit;

namespace TeamForge.Tests.Services;

public class OnboardingServiceTests : IDisposable
{
    private readonly TeamForgeDbContext _db;
    private readonly OnboardingService _sut;
    private readonly Mock<IAiAgentService> _aiAgentMock;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _adminUserId = Guid.NewGuid();

    public OnboardingServiceTests()
    {
        var options = new DbContextOptionsBuilder<TeamForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TeamForgeDbContext(options);

        _aiAgentMock = new Mock<IAiAgentService>();
        var logger = new Mock<ILogger<OnboardingService>>();
        _sut = new OnboardingService(_db, _aiAgentMock.Object, logger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var tenant = new Tenant { Id = _tenantId, CompanyName = "Test Corp" };
        _db.Tenants.Add(tenant);

        _db.TenantBranding.Add(new TenantBranding { TenantId = _tenantId });

        var user = new AppUser
        {
            Id = _adminUserId,
            TenantId = _tenantId,
            Email = "admin@test.com",
            DisplayName = "Admin",
            PasswordHash = "hashed"
        };
        _db.AppUsers.Add(user);

        _db.SaveChanges();
    }

    [Fact]
    public async Task GeneratePreviewAsync_DelegatesToAiAgent()
    {
        // Arrange
        var expected = new OnboardingPreview
        {
            Branding = new SuggestedBranding { PrimaryColor = "#ff0000" },
            Roles = new List<string> { "Admin", "Dev" },
            Teams = new List<SuggestedTeam> { new() { Name = "Engineering", Description = "Builds stuff" } },
            ProjectCategories = new List<string> { "Dev" },
            WelcomeAnnouncement = "Welcome!"
        };

        _aiAgentMock
            .Setup(x => x.GenerateOnboardingConfigAsync("Test", "A test company", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GeneratePreviewAsync("Test", "A test company");

        // Assert
        Assert.Equal("#ff0000", result.Branding.PrimaryColor);
        Assert.Equal(2, result.Roles.Count);
        Assert.Single(result.Teams);
    }

    [Fact]
    public async Task ConfirmOnboardingAsync_PersistsAllConfig()
    {
        // Arrange
        var request = new OnboardingConfirmRequest
        {
            TenantId = _tenantId,
            Config = new OnboardingPreview
            {
                Branding = new SuggestedBranding
                {
                    PrimaryColor = "#ff0000",
                    SecondaryColor = "#00ff00",
                    AccentColor = "#0000ff",
                    BackgroundColor = "#ffffff",
                    TextColor = "#000000",
                    FontFamily = "Arial",
                    TagLine = "Test tagline"
                },
                Roles = new List<string> { "Admin", "Developer", "Designer" },
                Teams = new List<SuggestedTeam>
                {
                    new() { Name = "Engineering", Description = "Builds things" },
                    new() { Name = "Design", Description = "Designs things" }
                },
                ProjectCategories = new List<string> { "Dev", "Design" },
                WelcomeAnnouncement = "Welcome to the team!"
            }
        };

        // Act
        await _sut.ConfirmOnboardingAsync(request);

        // Assert — branding updated
        var branding = await _db.TenantBranding.FirstOrDefaultAsync(b => b.TenantId == _tenantId);
        Assert.NotNull(branding);
        Assert.Equal("#ff0000", branding.PrimaryColor);
        Assert.Equal("Arial", branding.FontFamily);

        // Assert — roles created
        var roles = await _db.Roles.Where(r => r.TenantId == _tenantId).ToListAsync();
        Assert.Equal(3, roles.Count);

        // Assert — teams created
        var teams = await _db.Teams.Where(t => t.TenantId == _tenantId).ToListAsync();
        Assert.Equal(2, teams.Count);

        // Assert — announcement created
        var announcements = await _db.Announcements.Where(a => a.TenantId == _tenantId).ToListAsync();
        Assert.Single(announcements);
        Assert.Equal("Welcome to the team!", announcements[0].Content);
    }

    [Fact]
    public async Task ConfirmOnboardingAsync_NonexistentTenant_Throws()
    {
        var request = new OnboardingConfirmRequest
        {
            TenantId = Guid.NewGuid(),
            Config = new OnboardingPreview()
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ConfirmOnboardingAsync(request));
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
