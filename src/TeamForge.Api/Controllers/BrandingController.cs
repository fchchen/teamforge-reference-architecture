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
public class BrandingController : ControllerBase
{
    private readonly TeamForgeDbContext _db;
    private readonly ILogger<BrandingController> _logger;

    public BrandingController(TeamForgeDbContext db, ILogger<BrandingController> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [ProducesResponseType(typeof(BrandingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandingResponse>> GetBranding(CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var tenant = await _db.Tenants
            .Include(t => t.Branding)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant?.Branding is null)
            return NotFound();

        return Ok(MapToResponse(tenant));
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BrandingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandingResponse>> UpdateBranding(
        [FromBody] UpdateBrandingRequest request, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var tenant = await _db.Tenants
            .Include(t => t.Branding)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant?.Branding is null)
            return NotFound();

        var branding = tenant.Branding;
        if (request.PrimaryColor is not null) branding.PrimaryColor = request.PrimaryColor;
        if (request.SecondaryColor is not null) branding.SecondaryColor = request.SecondaryColor;
        if (request.AccentColor is not null) branding.AccentColor = request.AccentColor;
        if (request.BackgroundColor is not null) branding.BackgroundColor = request.BackgroundColor;
        if (request.TextColor is not null) branding.TextColor = request.TextColor;
        if (request.LogoUrl is not null) branding.LogoUrl = request.LogoUrl;
        if (request.FontFamily is not null) branding.FontFamily = request.FontFamily;
        if (request.TagLine is not null) branding.TagLine = request.TagLine;
        branding.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Branding updated for tenant {TenantId}", tenantId);
        return Ok(MapToResponse(tenant));
    }

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedAccessException("Missing tenant_id claim");
    }

    private static BrandingResponse MapToResponse(Data.Entities.Tenant tenant) => new()
    {
        TenantId = tenant.Id.ToString(),
        CompanyName = tenant.CompanyName,
        PrimaryColor = tenant.Branding!.PrimaryColor,
        SecondaryColor = tenant.Branding.SecondaryColor,
        AccentColor = tenant.Branding.AccentColor,
        BackgroundColor = tenant.Branding.BackgroundColor,
        TextColor = tenant.Branding.TextColor,
        LogoUrl = tenant.Branding.LogoUrl,
        FontFamily = tenant.Branding.FontFamily,
        TagLine = tenant.Branding.TagLine
    };
}
