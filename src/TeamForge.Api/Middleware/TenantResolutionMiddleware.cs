using System.Security.Claims;
using TeamForge.Data;

namespace TeamForge.Api.Middleware;

/// <summary>
/// Extracts tenant_id from JWT claims and sets SQL SESSION_CONTEXT for Row-Level Security.
/// Must run AFTER authentication (needs JWT claims) and BEFORE controllers (must set context before DB access).
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, TeamForgeDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value;

            if (Guid.TryParse(tenantClaim, out var tenantId))
            {
                // Set SESSION_CONTEXT for SQL Row-Level Security
                await db.SetTenantContextAsync(tenantId, context.RequestAborted);
                context.Items["TenantId"] = tenantId;

                _logger.LogDebug("Tenant context set to {TenantId}", tenantId);
            }
            else
            {
                _logger.LogWarning("Authenticated user missing valid tenant_id claim");
            }
        }

        await _next(context);
    }
}
