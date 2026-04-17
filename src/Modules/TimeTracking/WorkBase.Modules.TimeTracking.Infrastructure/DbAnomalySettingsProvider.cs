using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Contracts;

namespace WorkBase.Modules.TimeTracking.Infrastructure;

/// <summary>
/// Loads anomaly detection settings from the cfg_tenant_configs table.
/// Falls back to defaults if no tenant-specific row exists.
/// </summary>
public sealed class DbAnomalySettingsProvider(
    WorkBaseDbContext dbContext,
    ILogger<DbAnomalySettingsProvider> logger) : IAnomalySettingsProvider
{
    private const string ConfigKey = "anomaly_detection";

    public async Task<AnomalyDetectionSettings> GetSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var configValue = await dbContext.Database
                .SqlQueryRaw<string>(
                    "SELECT value FROM cfg_tenant_configs WHERE tenant_id = {0} AND key = {1} LIMIT 1",
                    tenantId, ConfigKey)
                .FirstOrDefaultAsync(cancellationToken);

            if (configValue is not null)
            {
                var settings = JsonSerializer.Deserialize<AnomalyDetectionSettings>(configValue);
                if (settings is not null)
                    return settings;
            }
        }
        catch (Exception ex)
        {
            // Table may not exist yet — fall back to defaults
            logger.LogDebug(ex,
                "Could not load anomaly settings for tenant {TenantId} — using defaults", tenantId);
        }

        return new AnomalyDetectionSettings();
    }
}
