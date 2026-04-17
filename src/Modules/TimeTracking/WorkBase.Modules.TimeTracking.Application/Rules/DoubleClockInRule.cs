using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Rules;

/// <summary>
/// Detects consecutive ClockIn entries without an intervening ClockOut.
/// </summary>
public sealed class DoubleClockInRule : IAnomalyRule
{
    public async Task<List<TimeAnomaly>> EvaluateAsync(AnomalyRuleContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Settings.DetectDoubleClockIn)
            return [];

        for (var i = 1; i < context.Entries.Count; i++)
        {
            if (context.Entries[i].Type == TimeEntryType.ClockIn &&
                context.Entries[i - 1].Type == TimeEntryType.ClockIn)
            {
                if (await context.AlreadyDetected(AnomalyType.DoubleClockIn, cancellationToken))
                    return [];

                return
                [
                    TimeAnomaly.Create(
                        context.TenantId, context.EmployeeId, context.Date,
                        AnomalyType.DoubleClockIn,
                        "Wykryto podwójną rejestrację wejścia.",
                        timeSheetId: context.TimeSheet?.Id)
                ];
            }
        }

        return [];
    }
}
