using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamForge.Api.Models;
using TeamForge.Data;

namespace TeamForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly TeamForgeDbContext _db;

    public DashboardController(TeamForgeDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken cancellationToken)
    {
        var projectCount = await _db.Projects.CountAsync(cancellationToken);
        var teamCount = await _db.Teams.CountAsync(cancellationToken);
        var userCount = await _db.AppUsers.CountAsync(u => u.IsActive, cancellationToken);

        var recentAnnouncements = await _db.Announcements
            .Include(a => a.CreatedBy)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .Select(a => new AnnouncementResponse
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                CreatedByName = a.CreatedBy.DisplayName,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var recentProjects = await _db.Projects
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new ProjectResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status,
                Category = p.Category,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new DashboardResponse
        {
            ProjectCount = projectCount,
            TeamCount = teamCount,
            UserCount = userCount,
            RecentAnnouncements = recentAnnouncements,
            RecentProjects = recentProjects
        });
    }
}
