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
public class RolesController : ControllerBase
{
    private readonly TeamForgeDbContext _db;

    public RolesController(TeamForgeDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RoleResponse>>> List(CancellationToken cancellationToken)
    {
        var roles = await _db.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleResponse
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description
            })
            .ToListAsync(cancellationToken);

        return Ok(roles);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<RoleResponse>> Create(
        [FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var role = new Role
        {
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);

        return Created("", new RoleResponse
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description
        });
    }

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedAccessException("Missing tenant_id claim");
    }
}
