using TeamForge.Api.Models;

namespace TeamForge.Api.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse?> DemoLoginAsync(string tenantName, CancellationToken cancellationToken = default);
    Task<AuthResponse?> RefreshTokenAsync(string token, CancellationToken cancellationToken = default);
}
