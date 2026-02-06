using Microsoft.AspNetCore.Mvc;
using TeamForge.Api.Models;
using TeamForge.Api.Services;

namespace TeamForge.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(IOnboardingService onboardingService, ILogger<OnboardingController> logger)
    {
        _onboardingService = onboardingService ?? throw new ArgumentNullException(nameof(onboardingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(OnboardingPreview), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OnboardingPreview>> Generate(
        [FromBody] OnboardingGenerateRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating onboarding config for {CompanyName}", request.CompanyName);

        var preview = await _onboardingService.GeneratePreviewAsync(
            request.CompanyName, request.CompanyDescription, cancellationToken);

        return Ok(preview);
    }

    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Confirm(
        [FromBody] OnboardingConfirmRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Confirming onboarding for tenant {TenantId}", request.TenantId);

        await _onboardingService.ConfirmOnboardingAsync(request, cancellationToken);

        return NoContent();
    }
}
