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
public class TeamsController : ControllerBase
{
    private readonly TeamForgeDbContext _db;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(TeamForgeDbContext db, ILogger<TeamsController> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TeamResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TeamResponse>>> List(CancellationToken cancellationToken)
    {
        var teams = await _db.Teams
            .Include(t => t.Members).ThenInclude(m => m.User)
            .OrderBy(t => t.Name)
            .Select(t => new TeamResponse
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                MemberCount = t.Members.Count,
                CreatedAt = t.CreatedAt,
                Members = t.Members.Select(m => new TeamMemberResponse
                {
                    UserId = m.UserId,
                    DisplayName = m.User.DisplayName,
                    Email = m.User.Email,
                    Role = m.Role
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(teams);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TeamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var team = await _db.Teams
            .Include(t => t.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (team is null) return NotFound();

        return Ok(new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            MemberCount = team.Members.Count,
            CreatedAt = team.CreatedAt,
            Members = team.Members.Select(m => new TeamMemberResponse
            {
                UserId = m.UserId,
                DisplayName = m.User.DisplayName,
                Email = m.User.Email,
                Role = m.Role
            }).ToList()
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(TeamResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<TeamResponse>> Create(
        [FromBody] CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var team = new Team
        {
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description
        };

        _db.Teams.Add(team);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = team.Id }, new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            MemberCount = 0,
            CreatedAt = team.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TeamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamResponse>> Update(Guid id,
        [FromBody] UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        var team = await _db.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (team is null) return NotFound();

        if (request.Name is not null) team.Name = request.Name;
        if (request.Description is not null) team.Description = request.Description;
        team.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            MemberCount = team.Members.Count,
            CreatedAt = team.CreatedAt
        });
    }

    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMember(Guid id,
        [FromBody] AddTeamMemberRequest request, CancellationToken cancellationToken)
    {
        var team = await _db.Teams.FindAsync(new object[] { id }, cancellationToken);
        if (team is null) return NotFound();

        var member = new TeamMember
        {
            TenantId = team.TenantId,
            TeamId = id,
            UserId = request.UserId,
            Role = request.Role
        };

        _db.TeamMembers.Add(member);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{teamId:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid teamId, Guid userId, CancellationToken cancellationToken)
    {
        var member = await _db.TeamMembers
            .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId, cancellationToken);

        if (member is null) return NotFound();

        _db.TeamMembers.Remove(member);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedAccessException("Missing tenant_id claim");
    }
}
