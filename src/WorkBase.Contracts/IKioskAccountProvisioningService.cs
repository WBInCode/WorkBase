namespace WorkBase.Contracts;

public interface IKioskAccountProvisioningService
{
    Task<KioskAccountProvisioningResult?> EnsureForTenantAsync(
        Guid tenantId,
        string administratorEmail,
        bool credentialsCanBeReturned,
        CancellationToken cancellationToken = default);
}

public sealed record KioskAccountProvisioningResult(
    string Username,
    string LoginUrl,
    string? TemporaryPassword,
    bool CredentialsEmailSent,
    bool CredentialsDelivered);
