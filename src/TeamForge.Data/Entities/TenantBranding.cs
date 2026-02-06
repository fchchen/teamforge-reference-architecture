using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamForge.Data.Entities;

[Table("TenantBranding", Schema = "tenant")]
public class TenantBranding
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    [StringLength(7)]
    public string PrimaryColor { get; set; } = "#1976d2";

    [StringLength(7)]
    public string SecondaryColor { get; set; } = "#ff9800";

    [StringLength(7)]
    public string AccentColor { get; set; } = "#4caf50";

    [StringLength(7)]
    public string BackgroundColor { get; set; } = "#fafafa";

    [StringLength(7)]
    public string TextColor { get; set; } = "#212121";

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    [StringLength(100)]
    public string FontFamily { get; set; } = "Roboto";

    [StringLength(200)]
    public string? TagLine { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant Tenant { get; set; } = null!;
}
