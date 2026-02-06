using TeamForge.Api.Models;

namespace TeamForge.Api.Services;

public class MockAiAgentService : IAiAgentService
{
    public Task<OnboardingPreview> GenerateOnboardingConfigAsync(
        string companyName,
        string companyDescription,
        CancellationToken cancellationToken = default)
    {
        var preview = new OnboardingPreview
        {
            Branding = new SuggestedBranding
            {
                PrimaryColor = "#1976d2",
                SecondaryColor = "#ff9800",
                AccentColor = "#4caf50",
                BackgroundColor = "#fafafa",
                TextColor = "#212121",
                FontFamily = "Roboto",
                TagLine = $"Powering {companyName}'s success"
            },
            Roles = new List<string> { "Admin", "Lead", "Member" },
            Teams = new List<SuggestedTeam>
            {
                new() { Name = "Engineering", Description = "Software development and technical operations" },
                new() { Name = "Design", Description = "UI/UX and visual design" },
                new() { Name = "Operations", Description = "Business operations and support" }
            },
            ProjectCategories = new List<string> { "Development", "Design", "Research", "Operations" },
            WelcomeAnnouncement = $"Welcome to {companyName}'s TeamForge portal! " +
                "We're excited to have you on board. Explore your teams, check out ongoing projects, " +
                "and stay connected with company announcements."
        };

        return Task.FromResult(preview);
    }
}
