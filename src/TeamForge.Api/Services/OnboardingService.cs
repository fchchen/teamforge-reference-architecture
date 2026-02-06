using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TeamForge.Api.Models;
using TeamForge.Data;
using TeamForge.Data.Entities;

namespace TeamForge.Api.Services;

public class OnboardingService : IOnboardingService
{
    private readonly TeamForgeDbContext _db;
    private readonly IAiAgentService _aiAgent;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(TeamForgeDbContext db, IAiAgentService aiAgent, ILogger<OnboardingService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _aiAgent = aiAgent ?? throw new ArgumentNullException(nameof(aiAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OnboardingPreview> GeneratePreviewAsync(string companyName, string companyDescription,
        CancellationToken cancellationToken = default)
    {
        return await _aiAgent.GenerateOnboardingConfigAsync(companyName, companyDescription, cancellationToken);
    }

    public async Task ConfirmOnboardingAsync(OnboardingConfirmRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants
            .Include(t => t.Branding)
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Tenant {request.TenantId} not found");

        var useTransaction = _db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
        IDbContextTransaction? transaction = useTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken) : null;

        try
        {
            // Update branding
            if (tenant.Branding is not null)
            {
                tenant.Branding.PrimaryColor = request.Config.Branding.PrimaryColor;
                tenant.Branding.SecondaryColor = request.Config.Branding.SecondaryColor;
                tenant.Branding.AccentColor = request.Config.Branding.AccentColor;
                tenant.Branding.BackgroundColor = request.Config.Branding.BackgroundColor;
                tenant.Branding.TextColor = request.Config.Branding.TextColor;
                tenant.Branding.FontFamily = request.Config.Branding.FontFamily;
                tenant.Branding.TagLine = request.Config.Branding.TagLine;
                tenant.Branding.UpdatedAt = DateTime.UtcNow;
            }

            // Create roles
            foreach (var roleName in request.Config.Roles)
            {
                var exists = await _db.Roles
                    .AnyAsync(r => r.TenantId == tenant.Id && r.Name == roleName, cancellationToken);

                if (!exists)
                {
                    _db.Roles.Add(new Role
                    {
                        TenantId = tenant.Id,
                        Name = roleName
                    });
                }
            }

            // Create teams
            foreach (var team in request.Config.Teams)
            {
                var exists = await _db.Teams
                    .AnyAsync(t => t.TenantId == tenant.Id && t.Name == team.Name, cancellationToken);

                if (!exists)
                {
                    _db.Teams.Add(new Team
                    {
                        TenantId = tenant.Id,
                        Name = team.Name,
                        Description = team.Description
                    });
                }
            }

            // Create welcome announcement
            var adminUser = await _db.AppUsers
                .FirstOrDefaultAsync(u => u.TenantId == tenant.Id, cancellationToken);

            if (adminUser is not null && !string.IsNullOrWhiteSpace(request.Config.WelcomeAnnouncement))
            {
                _db.Announcements.Add(new Announcement
                {
                    TenantId = tenant.Id,
                    Title = "Welcome to TeamForge!",
                    Content = request.Config.WelcomeAnnouncement,
                    CreatedByUserId = adminUser.Id
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Onboarding confirmed for tenant {TenantId}", request.TenantId);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }
}
