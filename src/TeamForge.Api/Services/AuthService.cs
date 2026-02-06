using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TeamForge.Api.Models;
using TeamForge.Data;

namespace TeamForge.Api.Services;

public class AuthService : IAuthService
{
    private readonly TeamForgeDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(TeamForgeDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.AppUsers
            .Include(u => u.Tenant)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null || user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var roleName = user.UserRoles.FirstOrDefault()?.Role.Name ?? "Member";
        return GenerateAuthResponse(user.Id, user.TenantId, user.Email, user.DisplayName,
            user.Tenant.CompanyName, roleName);
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Create tenant
        var tenant = new Data.Entities.Tenant
        {
            CompanyName = request.CompanyName
        };
        _db.Tenants.Add(tenant);

        // Create default branding
        var branding = new Data.Entities.TenantBranding { TenantId = tenant.Id };
        _db.TenantBranding.Add(branding);

        // Create admin role
        var adminRole = new Data.Entities.Role
        {
            TenantId = tenant.Id,
            Name = "Admin",
            Description = "Full access to all features"
        };
        _db.Roles.Add(adminRole);

        // Create member role
        var memberRole = new Data.Entities.Role
        {
            TenantId = tenant.Id,
            Name = "Member",
            Description = "Standard team member access"
        };
        _db.Roles.Add(memberRole);

        // Create user
        var user = new Data.Entities.AppUser
        {
            TenantId = tenant.Id,
            Email = request.Email,
            DisplayName = request.DisplayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            LastLoginAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);

        // Assign admin role
        _db.UserRoles.Add(new Data.Entities.UserRole
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            RoleId = adminRole.Id
        });

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New tenant registered: {CompanyName} by {Email}", request.CompanyName, request.Email);

        return GenerateAuthResponse(user.Id, tenant.Id, user.Email, user.DisplayName,
            tenant.CompanyName, "Admin");
    }

    public async Task<AuthResponse?> DemoLoginAsync(string tenantName, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.CompanyName == tenantName && t.IsActive, cancellationToken);

        if (tenant is null)
        {
            // Fall back to first active tenant
            tenant = await _db.Tenants
                .FirstOrDefaultAsync(t => t.IsActive, cancellationToken);
        }

        if (tenant is null)
        {
            _logger.LogWarning("No active tenant found for demo login");
            return null;
        }

        var user = await _db.AppUsers
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.TenantId == tenant.Id && u.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("No active user found for tenant {TenantName}", tenant.CompanyName);
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var roleName = user.UserRoles.FirstOrDefault()?.Role.Name ?? "Member";
        return GenerateAuthResponse(user.Id, tenant.Id, user.Email, user.DisplayName,
            tenant.CompanyName, roleName);
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var principal = ValidateToken(token);
        if (principal is null) return null;

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var uid)) return null;

        var user = await _db.AppUsers
            .Include(u => u.Tenant)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == uid && u.IsActive, cancellationToken);

        if (user is null) return null;

        var roleName = user.UserRoles.FirstOrDefault()?.Role.Name ?? "Member";
        return GenerateAuthResponse(user.Id, user.TenantId, user.Email, user.DisplayName,
            user.Tenant.CompanyName, roleName);
    }

    private AuthResponse GenerateAuthResponse(Guid userId, Guid tenantId, string email,
        string displayName, string tenantName, string role)
    {
        var expiration = DateTime.UtcNow.AddHours(8);
        var token = GenerateJwt(userId, tenantId, email, role, expiration);

        return new AuthResponse
        {
            Token = token,
            Email = email,
            DisplayName = displayName,
            TenantId = tenantId.ToString(),
            TenantName = tenantName,
            Role = role,
            Expiration = expiration
        };
    }

    private string GenerateJwt(Guid userId, Guid tenantId, string email, string role, DateTime expiration)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey()));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "TeamForge",
            audience: _config["Jwt:Audience"] ?? "TeamForgeClient",
            claims: claims,
            expires: expiration,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey()));

            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"] ?? "TeamForge",
                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"] ?? "TeamForgeClient",
                ValidateLifetime = false // Allow expired tokens for refresh
            }, out _);
        }
        catch
        {
            return null;
        }
    }

    private string GetJwtKey()
    {
        return _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
    }

    public async Task<EntraLoginResponse> EntraLoginAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var entraUser = await ValidateEntraTokenAsync(accessToken, cancellationToken);

        var user = await _db.AppUsers
            .Include(u => u.Tenant)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.EntraIdObjectId == entraUser.ObjectId, cancellationToken);

        if (user is null)
        {
            return new EntraLoginResponse { IsProvisioned = false };
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var roleName = user.UserRoles.FirstOrDefault()?.Role.Name ?? "Member";
        return new EntraLoginResponse
        {
            IsProvisioned = true,
            Auth = GenerateAuthResponse(user.Id, user.TenantId, user.Email, user.DisplayName,
                user.Tenant.CompanyName, roleName)
        };
    }

    public async Task<AuthResponse> EntraProvisionAsync(string accessToken, string companyName, string displayName, CancellationToken cancellationToken = default)
    {
        var entraUser = await ValidateEntraTokenAsync(accessToken, cancellationToken);

        // Create tenant
        var tenant = new Data.Entities.Tenant
        {
            CompanyName = companyName
        };
        _db.Tenants.Add(tenant);

        // Create default branding
        var branding = new Data.Entities.TenantBranding { TenantId = tenant.Id };
        _db.TenantBranding.Add(branding);

        // Create admin role
        var adminRole = new Data.Entities.Role
        {
            TenantId = tenant.Id,
            Name = "Admin",
            Description = "Full access to all features"
        };
        _db.Roles.Add(adminRole);

        // Create member role
        var memberRole = new Data.Entities.Role
        {
            TenantId = tenant.Id,
            Name = "Member",
            Description = "Standard team member access"
        };
        _db.Roles.Add(memberRole);

        // Create user (no password — Entra ID auth)
        var user = new Data.Entities.AppUser
        {
            TenantId = tenant.Id,
            Email = entraUser.Email,
            DisplayName = displayName,
            EntraIdObjectId = entraUser.ObjectId,
            LastLoginAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);

        // Assign admin role
        _db.UserRoles.Add(new Data.Entities.UserRole
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            RoleId = adminRole.Id
        });

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New Entra ID tenant provisioned: {CompanyName} by {Email}", companyName, entraUser.Email);

        return GenerateAuthResponse(user.Id, tenant.Id, user.Email, user.DisplayName,
            tenant.CompanyName, "Admin");
    }

    private async Task<EntraUserInfo> ValidateEntraTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        var instance = _config["AzureAd:Instance"] ?? "https://login.microsoftonline.com/";
        var tenantId = _config["AzureAd:TenantId"] ?? throw new InvalidOperationException("AzureAd:TenantId not configured");
        var clientId = _config["AzureAd:ClientId"] ?? throw new InvalidOperationException("AzureAd:ClientId not configured");
        var audience = _config["AzureAd:Audience"] ?? $"api://{clientId}";

        var authority = $"{instance.TrimEnd('/')}/{tenantId}/v2.0";
        var metadataAddress = $"{authority}/.well-known/openid-configuration";

        var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());

        var config = await configManager.GetConfigurationAsync(cancellationToken);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{instance.TrimEnd('/')}/{tenantId}/v2.0",
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = config.SigningKeys,
            ValidateLifetime = true
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(accessToken, validationParameters, out var validatedToken);

        var oid = principal.FindFirst("oid")?.Value
            ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? throw new SecurityTokenException("Missing oid claim");

        var email = principal.FindFirst("preferred_username")?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value
            ?? throw new SecurityTokenException("Missing email claim");

        var name = principal.FindFirst("name")?.Value ?? email;

        return new EntraUserInfo(oid, email, name);
    }

    private record EntraUserInfo(string ObjectId, string Email, string Name);
}
