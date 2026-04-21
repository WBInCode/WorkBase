using WorkBase.Modules.Integration.Domain.Enums;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Integration.Domain.Entities;

public sealed class OAuthToken : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public IntegrationProvider Provider { get; private set; }
    public string EncryptedAccessToken { get; private set; } = string.Empty;
    public string? EncryptedRefreshToken { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public string? Scopes { get; private set; }
    public TokenStatus Status { get; private set; }

    private OAuthToken() { }

    public static OAuthToken Create(
        Guid tenantId, Guid userId, IntegrationProvider provider,
        string encryptedAccessToken, string? encryptedRefreshToken,
        DateTime expiresAtUtc, string? scopes)
    {
        return new OAuthToken
        {
            TenantId = tenantId,
            UserId = userId,
            Provider = provider,
            EncryptedAccessToken = encryptedAccessToken,
            EncryptedRefreshToken = encryptedRefreshToken,
            ExpiresAtUtc = expiresAtUtc,
            Scopes = scopes,
            Status = TokenStatus.Active
        };
    }

    public void UpdateTokens(string encryptedAccessToken, string? encryptedRefreshToken, DateTime expiresAtUtc)
    {
        EncryptedAccessToken = encryptedAccessToken;
        EncryptedRefreshToken = encryptedRefreshToken;
        ExpiresAtUtc = expiresAtUtc;
        Status = TokenStatus.Active;
    }

    public void Revoke() => Status = TokenStatus.Revoked;

    public bool IsExpired => ExpiresAtUtc <= DateTime.UtcNow;
}
