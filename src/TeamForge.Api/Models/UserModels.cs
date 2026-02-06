using System.ComponentModel.DataAnnotations;

namespace TeamForge.Api.Models;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class InviteUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string RoleName { get; set; } = "Member";

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserRolesRequest
{
    [Required]
    public List<string> RoleNames { get; set; } = new();
}

public class AnnouncementResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateAnnouncementRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}

public class DashboardResponse
{
    public int ProjectCount { get; set; }
    public int TeamCount { get; set; }
    public int UserCount { get; set; }
    public List<AnnouncementResponse> RecentAnnouncements { get; set; } = new();
    public List<ProjectResponse> RecentProjects { get; set; } = new();
}

public class RoleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateRoleRequest
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }
}
