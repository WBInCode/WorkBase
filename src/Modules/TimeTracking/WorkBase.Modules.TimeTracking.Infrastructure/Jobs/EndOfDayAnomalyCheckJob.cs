using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Services;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Jobs;

public sealed class EndOfDayAnomalyCheckJob(
    WorkBaseDbContext dbContext,
    AnomalyDetectionService anomalyDetectionService,
    IAnomalySettingsProvider settingsProvider,
    ILogger<EndOfDayAnomalyCheckJob> logger)
{
    public async Task ExecuteAsync()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        logger.LogInformation("Starting anomaly detection for date {Date}", yesterday);

        var startUtc = yesterday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = yesterday.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Get distinct (tenant, employee) pairs that had time entries or schedules on that date
        var entryPairs = await dbContext.Set<TimeEntry>()
            .Where(e => e.EntryTime >= startUtc && e.EntryTime < endUtc)
            .Select(e => new { e.TenantId, e.EmployeeId })
            .Distinct()
            .ToListAsync();

        var schedulePairs = await dbContext.Set<Schedule>()
            .Where(s => s.Date == yesterday)
            .Select(s => new { s.TenantId, s.EmployeeId })
            .Distinct()
            .ToListAsync();

        var allPairs = entryPairs
            .Union(schedulePairs)
            .Distinct()
            .ToList();

        var totalAnomalies = 0;
        var tenantSettingsCache = new Dictionary<Guid, AnomalyDetectionSettings>();

        foreach (var pair in allPairs)
        {
            try
            {
                if (!tenantSettingsCache.TryGetValue(pair.TenantId, out var settings))
                {
                    settings = await settingsProvider.GetSettingsAsync(pair.TenantId);
                    tenantSettingsCache[pair.TenantId] = settings;
                }

                var anomalies = await anomalyDetectionService.DetectAnomaliesForDateAsync(
                    pair.TenantId, pair.EmployeeId, yesterday, settings);

                totalAnomalies += anomalies.Count;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error detecting anomalies for tenant {TenantId}, employee {EmployeeId} on {Date}",
                    pair.TenantId, pair.EmployeeId, yesterday);
            }
        }

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Anomaly detection completed for {Date}. Processed {EmployeeCount} employees, found {AnomalyCount} anomalies.",
            yesterday, allPairs.Count, totalAnomalies);
    }
}
