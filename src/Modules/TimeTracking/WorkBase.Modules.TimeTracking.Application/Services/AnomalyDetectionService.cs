using Microsoft.Extensions.Logging;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Services;

public sealed class AnomalyDetectionService(
    ITimeEntryRepository timeEntryRepository,
    IScheduleRepository scheduleRepository,
    ITimeAnomalyRepository anomalyRepository,
    ITimeSheetRepository timeSheetRepository,
    IEnumerable<IAnomalyRule> rules,
    ILogger<AnomalyDetectionService> logger)
{
    public async Task<List<TimeAnomaly>> DetectAnomaliesForDateAsync(
        Guid tenantId,
        Guid employeeId,
        DateOnly date,
        AnomalyDetectionSettings settings,
        CancellationToken cancellationToken = default)
    {
        var entries = await timeEntryRepository.GetEntriesForDateAsync(
            tenantId, employeeId, date, cancellationToken);

        var schedule = await scheduleRepository.GetByDateAsync(
            tenantId, employeeId, date, cancellationToken);

        var timeSheet = await timeSheetRepository.GetByDateAsync(
            tenantId, employeeId, date, cancellationToken);

        var ordered = entries.OrderBy(e => e.EntryTime).ToList();

        var context = new AnomalyRuleContext
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            Date = date,
            Entries = ordered,
            Schedule = schedule,
            TimeSheet = timeSheet,
            Settings = settings,
            AlreadyDetected = (type, ct) => anomalyRepository.ExistsAsync(tenantId, employeeId, date, type, ct),
        };

        var anomalies = new List<TimeAnomaly>();

        foreach (var rule in rules)
        {
            try
            {
                var detected = await rule.EvaluateAsync(context, cancellationToken);
                anomalies.AddRange(detected);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Rule {RuleName} failed for employee {EmployeeId} on {Date}",
                    rule.GetType().Name, employeeId, date);
            }
        }

        // Persist new anomalies
        foreach (var anomaly in anomalies)
        {
            await anomalyRepository.AddAsync(anomaly, cancellationToken);
        }

        if (anomalies.Count > 0)
        {
            logger.LogInformation(
                "Detected {Count} anomalies for employee {EmployeeId} on {Date}",
                anomalies.Count, employeeId, date);
        }

        return anomalies;
    }
}
