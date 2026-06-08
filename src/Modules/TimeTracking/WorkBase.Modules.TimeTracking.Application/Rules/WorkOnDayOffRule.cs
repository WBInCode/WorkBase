using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Rules;

public sealed class WorkOnDayOffRule : IAnomalyRule
{
    public async Task<List<TimeAnomaly>> EvaluateAsync(AnomalyRuleContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Settings.DetectWorkOnDayOff) return [];
        if (context.Schedule is not null || context.Entries.Count == 0) return [];

        // Has clock-in entries but no schedule — work on a day off
        var hasClockIn = context.Entries.Any(e => e.Type == TimeEntryType.ClockIn);
        if (!hasClockIn) return [];

        if (await context.AlreadyDetected(AnomalyType.WorkOnDayOff, cancellationToken)) return [];

        return [
            TimeAnomaly.Create(context.TenantId, context.EmployeeId, context.Date,
                AnomalyType.WorkOnDayOff,
                "Wykryto pracę w dniu bez zaplanowanego grafiku.",
                timeSheetId: context.TimeSheet?.Id)
        ];
    }
}
