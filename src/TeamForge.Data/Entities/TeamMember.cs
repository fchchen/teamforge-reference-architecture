using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamForge.Data.Entities;

[Table("TeamMembers", Schema = "tenant")]
public class TeamMember
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }

    [StringLength(50)]
    public string Role { get; set; } = "Member";

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(TeamId))]
    public virtual Team Team { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual AppUser User { get; set; } = null!;
}
