using Microsoft.Extensions.Logging;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Services;

public sealed class AnomalyDetectionService(
    ITimeEntryRepository timeEntryRepository,
    IScheduleRepository scheduleRepository,
    ITimeAnomalyRepository anomalyRepository,
    ITimeSheetRepository timeSheetRepository,
    ILogger<AnomalyDetectionService> logger)
{
    public async Task<List<TimeAnomaly>> DetectAnomaliesForDateAsync(
        Guid tenantId,
        Guid employeeId,
        DateOnly date,
        AnomalyDetectionSettings settings,
        CancellationToken cancellationToken = default)
    {
        var anomalies = new List<TimeAnomaly>();

        var entries = await timeEntryRepository.GetEntriesForDateAsync(
            tenantId, employeeId, date, cancellationToken);

        var schedule = await scheduleRepository.GetByDateAsync(
            tenantId, employeeId, date, cancellationToken);

        var timeSheet = await timeSheetRepository.GetByDateAsync(
            tenantId, employeeId, date, cancellationToken);

        var ordered = entries.OrderBy(e => e.EntryTime).ToList();

        // Rule 1: Missing clock-out
        if (settings.DetectMissingClockOut)
        {
            var lastEntry = ordered.LastOrDefault();
            if (lastEntry is not null && lastEntry.Type is TimeEntryType.ClockIn or TimeEntryType.BreakEnd)
            {
                if (!await AlreadyDetectedAsync(tenantId, employeeId, date, AnomalyType.MissingClockOut, cancellationToken))
                {
                    anomalies.Add(TimeAnomaly.Create(
                        tenantId, employeeId, date,
                        AnomalyType.MissingClockOut,
                        "Pracownik nie zarejestrował wyjścia.",
                        timeSheetId: timeSheet?.Id));
                }
            }
        }

        // Rule 2: Missing clock-in (schedule exists but no entries)
        if (settings.DetectMissingClockIn && schedule is not null && ordered.Count == 0)
        {
            if (!await AlreadyDetectedAsync(tenantId, employeeId, date, AnomalyType.MissingClockIn, cancellationToken))
            {
                anomalies.Add(TimeAnomaly.Create(
                    tenantId, employeeId, date,
                    AnomalyType.MissingClockIn,
                    $"Brak rejestracji czasu pracy mimo zaplanowanej zmiany ({schedule.PlannedStart}–{schedule.PlannedEnd}).",
                    timeSheetId: timeSheet?.Id));
            }
        }

        // Rule 3: Late arrival
        if (settings.DetectLateArrival && schedule is not null)
        {
            var firstClockIn = ordered.FirstOrDefault(e => e.Type == TimeEntryType.ClockIn);
            if (firstClockIn is not null)
            {
                var clockInTime = TimeOnly.FromDateTime(firstClockIn.EntryTime);
                var delay = clockInTime - schedule.PlannedStart;

                if (delay > settings.LateArrivalThreshold)
                {
                    if (!await AlreadyDetectedAsync(tenantId, employeeId, date, AnomalyType.LateArrival, cancellationToken))
                    {
                        anomalies.Add(TimeAnomaly.Create(
                            tenantId, employeeId, date,
                            AnomalyType.LateArrival,
                            $"Spóźnienie {delay.TotalMinutes:F0} min (próg: {settings.LateArrivalThreshold.TotalMinutes:F0} min). Plan: {schedule.PlannedStart}, rzeczywiste: {clockInTime}.",
                            System.Text.Json.JsonSerializer.Serialize(new { PlannedStart = schedule.PlannedStart.ToString(), ActualStart = clockInTime.ToString(), DelayMinutes = delay.TotalMinutes }),
                            timeSheet?.Id));
                    }
                }
            }
        }

        // Rule 4: Double clock-in
        if (settings.DetectDoubleClockIn)
        {
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].Type == TimeEntryType.ClockIn && ordered[i - 1].Type == TimeEntryType.ClockIn)
                {
                    if (!await AlreadyDetectedAsync(tenantId, employeeId, date, AnomalyType.DoubleClockIn, cancellationToken))
                    {
                        anomalies.Add(TimeAnomaly.Create(
                            tenantId, employeeId, date,
                            AnomalyType.DoubleClockIn,
                            "Wykryto podwójną rejestrację wejścia.",
                            timeSheetId: timeSheet?.Id));
                    }
                    break;
                }
            }
        }

        // Rule 5: Excessive shift
        if (settings.DetectExcessiveShift && timeSheet is not null)
        {
            if (timeSheet.TotalWorked > settings.ExcessiveShiftThreshold)
            {
                if (!await AlreadyDetectedAsync(tenantId, employeeId, date, AnomalyType.ExcessiveShift, cancellationToken))
                {
                    anomalies.Add(TimeAnomaly.Create(
                        tenantId, employeeId, date,
                        AnomalyType.ExcessiveShift,
                        $"Zmiana trwała {timeSheet.TotalWorked.TotalHours:F1}h (próg: {settings.ExcessiveShiftThreshold.TotalHours:F0}h).",
                        System.Text.Json.JsonSerializer.Serialize(new { WorkedHours = timeSheet.TotalWorked.TotalHours, ThresholdHours = settings.ExcessiveShiftThreshold.TotalHours }),
                        timeSheet.Id));
                }
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

    private async Task<bool> AlreadyDetectedAsync(
        Guid tenantId, Guid employeeId, DateOnly date, AnomalyType type,
        CancellationToken cancellationToken)
    {
        return await anomalyRepository.ExistsAsync(tenantId, employeeId, date, type, cancellationToken);
    }
}
