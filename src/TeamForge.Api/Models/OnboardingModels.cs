using System.ComponentModel.DataAnnotations;

namespace TeamForge.Api.Models;

public class OnboardingGenerateRequest
{
    [Required]
    [StringLength(2000)]
    public string CompanyDescription { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;
}

public class OnboardingPreview
{
    public SuggestedBranding Branding { get; set; } = new();
    public List<string> Roles { get; set; } = new();
    public List<SuggestedTeam> Teams { get; set; } = new();
    public List<string> ProjectCategories { get; set; } = new();
    public string WelcomeAnnouncement { get; set; } = string.Empty;
}

public class SuggestedBranding
{
    public string PrimaryColor { get; set; } = "#1976d2";
    public string SecondaryColor { get; set; } = "#ff9800";
    public string AccentColor { get; set; } = "#4caf50";
    public string BackgroundColor { get; set; } = "#fafafa";
    public string TextColor { get; set; } = "#212121";
    public string FontFamily { get; set; } = "Roboto";
    public string TagLine { get; set; } = string.Empty;
}

public class SuggestedTeam
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class OnboardingConfirmRequest
{
    [Required]
    public Guid TenantId { get; set; }

    [Required]
    public OnboardingPreview Config { get; set; } = new();
}
