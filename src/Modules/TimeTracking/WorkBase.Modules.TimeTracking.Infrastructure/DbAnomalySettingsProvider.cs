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
            // EF Core nazywa kolumne wyniku skalarnego "Value" i cytuje ja w wygenerowanym
            // zapytaniu zewnetrznym. Bez aliasu Postgres zwraca "value" i odczyt pada, a najemca
            // po cichu dostaje ustawienia domyslne mimo zapisanej konfiguracji.
            var configValue = await dbContext.Database
                .SqlQueryRaw<string>(
                    "SELECT value AS \"Value\" FROM cfg_tenant_configs WHERE tenant_id = {0} AND key = {1} LIMIT 1",
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
            // Ostrzezenie, nie debug: zejscie na ustawienia domyslne zmienia wyniki wykrywania
            // anomalii, wiec musi byc widoczne. Poprzedni poziom ukrywal blad przez tygodnie.
            logger.LogWarning(ex,
                "Nie udalo sie odczytac ustawien wykrywania anomalii dla najemcy {TenantId}, uzywam domyslnych", tenantId);
        }

        return new AnomalyDetectionSettings();
    }
}
