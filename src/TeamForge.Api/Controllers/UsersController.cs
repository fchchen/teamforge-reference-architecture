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
public class UsersController : ControllerBase
{
    private readonly TeamForgeDbContext _db;
    private readonly ILogger<UsersController> _logger;

    public UsersController(TeamForgeDbContext db, ILogger<UsersController> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserResponse>>> List(CancellationToken cancellationToken)
    {
        var users = await _db.AppUsers
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                IsActive = u.IsActive,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost("invite")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Invite(
        [FromBody] InviteUserRequest request, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();

        var exists = await _db.AppUsers
            .AnyAsync(u => u.Email == request.Email, cancellationToken);
        if (exists)
            return Conflict(new ProblemDetails { Title = "User with this email already exists" });

        var user = new AppUser
        {
            TenantId = tenantId,
            Email = request.Email,
            DisplayName = request.DisplayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };
        _db.AppUsers.Add(user);

        var role = await _db.Roles
            .FirstOrDefaultAsync(r => r.Name == request.RoleName, cancellationToken);
        if (role is not null)
        {
            _db.UserRoles.Add(new UserRole
            {
                TenantId = tenantId,
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Created("", new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            Roles = role is not null ? new List<string> { role.Name } : new List<string>(),
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPut("{id:guid}/roles")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoles(Guid id,
        [FromBody] UpdateUserRolesRequest request, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var user = await _db.AppUsers
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null) return NotFound();

        // Remove existing roles
        _db.UserRoles.RemoveRange(user.UserRoles);

        // Add new roles
        foreach (var roleName in request.RoleNames)
        {
            var role = await _db.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
            if (role is not null)
            {
                _db.UserRoles.Add(new UserRole
                {
                    TenantId = tenantId,
                    UserId = user.Id,
                    RoleId = role.Id
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedAccessException("Missing tenant_id claim");
    }
}
