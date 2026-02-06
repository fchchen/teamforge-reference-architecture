using TeamForge.Api.Models;

namespace TeamForge.Api.Services;

public interface IOnboardingService
{
    Task<OnboardingPreview> GeneratePreviewAsync(string companyName, string companyDescription,
        CancellationToken cancellationToken = default);
    Task ConfirmOnboardingAsync(OnboardingConfirmRequest request,
        CancellationToken cancellationToken = default);
}
