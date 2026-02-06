using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TeamForge.Api.Models;
using TeamForge.Api.Services;

namespace TeamForge.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        if (result is null)
            return Unauthorized(new ProblemDetails { Title = "Invalid credentials" });

        return Ok(result);
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        if (result is null)
            return BadRequest(new ProblemDetails { Title = "Registration failed" });

        return Created("", result);
    }

    [HttpPost("demo")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthResponse>> DemoLogin(
        [FromBody] DemoLoginRequest? request, CancellationToken cancellationToken)
    {
        var tenantName = request?.TenantName ?? "Acme Corp";
        var result = await _authService.DemoLoginAsync(tenantName, cancellationToken);
        if (result is null)
            return NotFound(new ProblemDetails { Title = "No demo tenant available" });

        return Ok(result);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request.Token, cancellationToken);
        if (result is null)
            return Unauthorized(new ProblemDetails { Title = "Invalid or expired token" });

        return Ok(result);
    }

    [HttpPost("entra-login")]
    [ProducesResponseType(typeof(EntraLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EntraLoginResponse>> EntraLogin(
        [FromBody] EntraLoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.EntraLoginAsync(request.AccessToken, cancellationToken);
            return Ok(result);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Entra ID token validation failed");
            return Unauthorized(new ProblemDetails { Title = "Invalid Entra ID token" });
        }
    }

    [HttpPost("entra-provision")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> EntraProvision(
        [FromBody] EntraProvisionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.EntraProvisionAsync(
                request.AccessToken, request.CompanyName, request.DisplayName, cancellationToken);
            return Created("", result);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Entra ID token validation failed during provisioning");
            return Unauthorized(new ProblemDetails { Title = "Invalid Entra ID token" });
        }
    }
}
