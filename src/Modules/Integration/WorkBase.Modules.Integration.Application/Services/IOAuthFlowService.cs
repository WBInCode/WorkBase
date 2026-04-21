using WorkBase.Modules.Integration.Domain.Enums;

namespace WorkBase.Modules.Integration.Application.Services;

public sealed record OAuthCallbackResult(
    string AccessToken,
    string? RefreshToken,
    DateTime ExpiresAtUtc,
    string? ExternalAccountId,
    string? DisplayName);

public interface IOAuthFlowService
{
    string GetAuthorizationUrl(IntegrationProvider provider, Guid tenantId, Guid userId, string redirectUri, string? scopes = null);
    Task<OAuthCallbackResult> ExchangeCodeAsync(IntegrationProvider provider, string code, string redirectUri, CancellationToken ct = default);
    Task<OAuthCallbackResult> RefreshTokenAsync(IntegrationProvider provider, string refreshToken, CancellationToken ct = default);
}
