using System.ComponentModel.DataAnnotations;

namespace TeamForge.Api.Models;

public class BrandingResponse
{
    public string TenantId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = string.Empty;
    public string SecondaryColor { get; set; } = string.Empty;
    public string AccentColor { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string FontFamily { get; set; } = string.Empty;
    public string? TagLine { get; set; }
}

public class UpdateBrandingRequest
{
    [StringLength(7)]
    public string? PrimaryColor { get; set; }

    [StringLength(7)]
    public string? SecondaryColor { get; set; }

    [StringLength(7)]
    public string? AccentColor { get; set; }

    [StringLength(7)]
    public string? BackgroundColor { get; set; }

    [StringLength(7)]
    public string? TextColor { get; set; }

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    [StringLength(100)]
    public string? FontFamily { get; set; }

    [StringLength(200)]
    public string? TagLine { get; set; }
}
