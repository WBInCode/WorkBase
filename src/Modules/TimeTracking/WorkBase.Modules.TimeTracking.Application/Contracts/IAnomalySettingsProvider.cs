namespace WorkBase.Modules.TimeTracking.Application.Contracts;

/// <summary>
/// Loads anomaly detection settings for a specific tenant.
/// Falls back to defaults if no tenant-specific configuration exists.
/// </summary>
public interface IAnomalySettingsProvider
{
    Task<AnomalyDetectionSettings> GetSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
