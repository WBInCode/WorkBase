using System.Text.Json;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Rules;

/// <summary>
/// Detects late arrival by comparing first clock-in against the scheduled start time.
/// </summary>
public sealed class LateArrivalRule : IAnomalyRule
{
    public async Task<List<TimeAnomaly>> EvaluateAsync(AnomalyRuleContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Settings.DetectLateArrival || context.Schedule is null)
            return [];

        var firstClockIn = context.Entries.FirstOrDefault(e => e.Type == TimeEntryType.ClockIn);
        if (firstClockIn is null)
            return [];

        var clockInTime = TimeOnly.FromDateTime(firstClockIn.EntryTime);
        var delay = clockInTime - context.Schedule.PlannedStart;

        if (delay <= context.Settings.LateArrivalThreshold)
            return [];

        if (await context.AlreadyDetected(AnomalyType.LateArrival, cancellationToken))
            return [];

        var details = JsonSerializer.Serialize(new
        {
            PlannedStart = context.Schedule.PlannedStart.ToString(),
            ActualStart = clockInTime.ToString(),
            DelayMinutes = delay.TotalMinutes
        });

        return
        [
            TimeAnomaly.Create(
                context.TenantId, context.EmployeeId, context.Date,
                AnomalyType.LateArrival,
                $"Spóźnienie {delay.TotalMinutes:F0} min (próg: {context.Settings.LateArrivalThreshold.TotalMinutes:F0} min). Plan: {context.Schedule.PlannedStart}, rzeczywiste: {clockInTime}.",
                details,
                context.TimeSheet?.Id)
        ];
    }
}
