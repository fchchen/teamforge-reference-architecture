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
public class ProjectsController : ControllerBase
{
    private readonly TeamForgeDbContext _db;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(TeamForgeDbContext db, ILogger<ProjectsController> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ProjectResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProjectResponse>>> List(CancellationToken cancellationToken)
    {
        // RLS automatically filters by tenant
        var projects = await _db.Projects
            .OrderByDescending(p => p.CreatedAt)
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

        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FindAsync(new object[] { id }, cancellationToken);
        if (project is null) return NotFound();

        return Ok(new ProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status,
            Category = project.Category,
            CreatedAt = project.CreatedAt
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProjectResponse>> Create(
        [FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var project = new Project
        {
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Project {ProjectName} created for tenant {TenantId}", project.Name, tenantId);

        return CreatedAtAction(nameof(Get), new { id = project.Id }, new ProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status,
            Category = project.Category,
            CreatedAt = project.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> Update(Guid id,
        [FromBody] UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FindAsync(new object[] { id }, cancellationToken);
        if (project is null) return NotFound();

        if (request.Name is not null) project.Name = request.Name;
        if (request.Description is not null) project.Description = request.Description;
        if (request.Status is not null) project.Status = request.Status;
        if (request.Category is not null) project.Category = request.Category;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new ProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status,
            Category = project.Category,
            CreatedAt = project.CreatedAt
        });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FindAsync(new object[] { id }, cancellationToken);
        if (project is null) return NotFound();

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedAccessException("Missing tenant_id claim");
    }
}
