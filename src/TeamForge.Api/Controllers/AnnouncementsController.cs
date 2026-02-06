using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamForge.Api.Models;
using TeamForge.Data;
using TeamForge.Data.Entities;

namespace TeamForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AnnouncementsController : ControllerBase
{
    private readonly TeamForgeDbContext _db;

    public AnnouncementsController(TeamForgeDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<AnnouncementResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AnnouncementResponse>>> List(CancellationToken cancellationToken)
    {
        var announcements = await _db.Announcements
            .Include(a => a.CreatedBy)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AnnouncementResponse
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                CreatedByName = a.CreatedBy.DisplayName,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(announcements);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AnnouncementResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AnnouncementResponse>> Create(
        [FromBody] CreateAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();

        var announcement = new Announcement
        {
            TenantId = tenantId,
            Title = request.Title,
            Content = request.Content,
            CreatedByUserId = userId
        };

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync(cancellationToken);

        var user = await _db.AppUsers.FindAsync(new object[] { userId }, cancellationToken);

        return Created("", new AnnouncementResponse
        {
            Id = announcement.Id,
            Title = announcement.Title,
            Content = announcement.Content,
            CreatedByName = user?.DisplayName ?? "Unknown",
            CreatedAt = announcement.CreatedAt
        });
    }

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedAccessException("Missing tenant_id claim");
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedAccessException("Missing user id claim");
    }
}
