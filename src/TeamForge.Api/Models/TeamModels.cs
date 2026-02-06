using System.ComponentModel.DataAnnotations;

namespace TeamForge.Api.Models;

public class TeamResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TeamMemberResponse> Members { get; set; } = new();
}

public class TeamMemberResponse
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class CreateTeamRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public class UpdateTeamRequest
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}

public class AddTeamMemberRequest
{
    [Required]
    public Guid UserId { get; set; }

    [StringLength(50)]
    public string Role { get; set; } = "Member";
}
