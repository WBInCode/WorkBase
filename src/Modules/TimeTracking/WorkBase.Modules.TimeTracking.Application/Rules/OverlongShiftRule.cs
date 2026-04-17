using System.Text.Json;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Rules;

/// <summary>
/// Detects shifts that exceed a configurable duration threshold.
/// </summary>
public sealed class OverlongShiftRule : IAnomalyRule
{
    public async Task<List<TimeAnomaly>> EvaluateAsync(AnomalyRuleContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Settings.DetectExcessiveShift || context.TimeSheet is null)
            return [];

        if (context.TimeSheet.TotalWorked <= context.Settings.ExcessiveShiftThreshold)
            return [];

        if (await context.AlreadyDetected(AnomalyType.ExcessiveShift, cancellationToken))
            return [];

        var details = JsonSerializer.Serialize(new
        {
            WorkedHours = context.TimeSheet.TotalWorked.TotalHours,
            ThresholdHours = context.Settings.ExcessiveShiftThreshold.TotalHours
        });

        return
        [
            TimeAnomaly.Create(
                context.TenantId, context.EmployeeId, context.Date,
                AnomalyType.ExcessiveShift,
                $"Zmiana trwała {context.TimeSheet.TotalWorked.TotalHours:F1}h (próg: {context.Settings.ExcessiveShiftThreshold.TotalHours:F0}h).",
                details,
                context.TimeSheet.Id)
        ];
    }
}
