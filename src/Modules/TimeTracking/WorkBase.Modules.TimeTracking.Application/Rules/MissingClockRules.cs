using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Rules;

/// <summary>
/// Detects when an employee clocked in but never clocked out.
/// </summary>
public sealed class MissingClockOutRule : IAnomalyRule
{
    public async Task<List<TimeAnomaly>> EvaluateAsync(AnomalyRuleContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Settings.DetectMissingClockOut)
            return [];

        var lastEntry = context.Entries.LastOrDefault();
        if (lastEntry is null || lastEntry.Type is not (TimeEntryType.ClockIn or TimeEntryType.BreakEnd))
            return [];

        if (await context.AlreadyDetected(AnomalyType.MissingClockOut, cancellationToken))
            return [];

        return
        [
            TimeAnomaly.Create(
                context.TenantId, context.EmployeeId, context.Date,
                AnomalyType.MissingClockOut,
                "Pracownik nie zarejestrował wyjścia.",
                timeSheetId: context.TimeSheet?.Id)
        ];
    }
}

/// <summary>
/// Detects when an employee had a schedule but registered no time entries.
/// </summary>
public sealed class MissingClockInRule : IAnomalyRule
{
    public async Task<List<TimeAnomaly>> EvaluateAsync(AnomalyRuleContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Settings.DetectMissingClockIn)
            return [];

        if (context.Schedule is null || context.Entries.Count != 0)
            return [];

        if (await context.AlreadyDetected(AnomalyType.MissingClockIn, cancellationToken))
            return [];

        return
        [
            TimeAnomaly.Create(
                context.TenantId, context.EmployeeId, context.Date,
                AnomalyType.MissingClockIn,
                $"Brak rejestracji czasu pracy mimo zaplanowanej zmiany ({context.Schedule.PlannedStart}–{context.Schedule.PlannedEnd}).",
                timeSheetId: context.TimeSheet?.Id)
        ];
    }
}
