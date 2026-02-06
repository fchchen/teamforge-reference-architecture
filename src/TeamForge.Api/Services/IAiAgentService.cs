using TeamForge.Api.Models;

namespace TeamForge.Api.Services;

public interface IAiAgentService
{
    Task<OnboardingPreview> GenerateOnboardingConfigAsync(
        string companyName,
        string companyDescription,
        CancellationToken cancellationToken = default);
}
