using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed class GetTimeSheetHandler(
    ITimeSheetRepository timeSheetRepository,
    ITimeEntryRepository timeEntryRepository)
    : IQueryHandler<GetTimeSheetQuery, TimeSheetPeriodDto>
{
    public async Task<Result<TimeSheetPeriodDto>> Handle(
        GetTimeSheetQuery request,
        CancellationToken cancellationToken)
    {
        if (request.From > request.To)
            return Result.Failure<TimeSheetPeriodDto>(Error.Validation(
                "TimeSheet.InvalidRange",
                "Data początkowa nie może być późniejsza niż data końcowa."));

        var maxDays = request.Period switch
        {
            "week" => 7,
            "month" => 31,
            _ => 31
        };

        if (request.To.DayNumber - request.From.DayNumber + 1 > maxDays)
            return Result.Failure<TimeSheetPeriodDto>(Error.Validation(
                "TimeSheet.RangeTooLarge",
                $"Zakres nie może przekraczać {maxDays} dni."));

        var timeSheets = await timeSheetRepository.GetByDateRangeAsync(
            request.TenantId, request.EmployeeId, request.From, request.To, cancellationToken);

        var sheetsByDate = timeSheets.ToDictionary(ts => ts.Date);

        // For days without a saved TimeSheet, calculate on-demand from entries
        var days = new List<TimeSheetDayDto>();
        var totalWorked = TimeSpan.Zero;
        var totalBreaks = TimeSpan.Zero;
        var daysWorked = 0;
        var daysIncomplete = 0;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (var date = request.From; date <= request.To; date = date.AddDays(1))
        {
            // Always recalculate today from live entries (persisted sheet may be stale)
            if (date == today || !sheetsByDate.TryGetValue(date, out var sheet))
            {
                var entries = await timeEntryRepository.GetEntriesForDateAsync(
                    request.TenantId, request.EmployeeId, date, cancellationToken);

                if (entries.Count > 0)
                {
                    var (worked, breaks) = CalculateWorkedTime(entries);
                    var entryDtos = MapEntries(entries);

                    var lastEntry = entries.OrderByDescending(e => e.EntryTime).First();
                    var hasClockOut = lastEntry.Type == TimeEntryType.ClockOut;
                    var status = hasClockOut ? "complete" : "incomplete";

                    days.Add(new TimeSheetDayDto(date, worked, breaks, worked - breaks, status, null, entryDtos));

                    totalWorked += worked;
                    totalBreaks += breaks;

                    if (hasClockOut)
                        daysWorked++;
                    else
                        daysIncomplete++;
                }
                else
                {
                    days.Add(new TimeSheetDayDto(date, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, "empty", null, []));
                }
            }
            else
            {
                // For persisted sheets (past days), also load entries for the timeline
                var entries = await timeEntryRepository.GetEntriesForDateAsync(
                    request.TenantId, request.EmployeeId, date, cancellationToken);
                var entryDtos = MapEntries(entries);

                // Recalculate from live entries if available (persisted sheet may be stale)
                TimeSpan worked, breaks;
                string status;

                if (entries.Count > 0)
                {
                    (worked, breaks) = CalculateWorkedTime(entries);
                    var lastEntry = entries.OrderByDescending(e => e.EntryTime).First();
                    var hasClockOut = lastEntry.Type == TimeEntryType.ClockOut;
                    status = hasClockOut ? "complete" : "incomplete";
                }
                else
                {
                    worked = sheet.TotalWorked;
                    breaks = sheet.TotalBreaks;
                    status = sheet.Status.ToString().ToLowerInvariant();
                }

                days.Add(new TimeSheetDayDto(
                    sheet.Date,
                    worked,
                    breaks,
                    worked - breaks,
                    status,
                    sheet.Note,
                    entryDtos));

                totalWorked += worked;
                totalBreaks += breaks;

                if (status is "complete" or "approved")
                    daysWorked++;
                else
                    daysIncomplete++;
            }
        }

        var netWorked = totalWorked - totalBreaks;

        return new TimeSheetPeriodDto(
            request.From,
            request.To,
            request.Period,
            request.EmployeeId,
            totalWorked,
            totalBreaks,
            netWorked,
            daysWorked,
            daysIncomplete,
            days);
    }

    private static IReadOnlyList<TimeSheetEntryDto> MapEntries(List<TimeEntry> entries)
    {
        return entries
            .OrderBy(e => e.EntryTime)
            .Select(e => new TimeSheetEntryDto(
                e.Id,
                e.EntryTime,
                e.Type.ToString(),
                e.BreakType?.ToString()))
            .ToList();
    }

    private static (TimeSpan TotalWorked, TimeSpan TotalBreaks) CalculateWorkedTime(List<TimeEntry> entries)
    {
        var ordered = entries.OrderBy(e => e.EntryTime).ToList();
        var now = DateTime.UtcNow;

        var totalWorked = TimeSpan.Zero;
        var totalBreaks = TimeSpan.Zero;

        DateTime? clockInTime = null;
        DateTime? breakStartTime = null;

        foreach (var entry in ordered)
        {
            switch (entry.Type)
            {
                case TimeEntryType.ClockIn:
                    clockInTime = entry.EntryTime;
                    break;

                case TimeEntryType.BreakStart:
                    breakStartTime = entry.EntryTime;
                    break;

                case TimeEntryType.BreakEnd:
                    if (breakStartTime.HasValue)
                    {
                        totalBreaks += entry.EntryTime - breakStartTime.Value;
                        breakStartTime = null;
                    }
                    break;

                case TimeEntryType.ClockOut:
                    if (clockInTime.HasValue)
                    {
                        totalWorked += entry.EntryTime - clockInTime.Value;
                        clockInTime = null;
                    }
                    break;
            }
        }

        // If still on break, count up to now
        if (breakStartTime.HasValue)
            totalBreaks += now - breakStartTime.Value;

        // If still working (no clock-out yet), count up to now
        if (clockInTime.HasValue)
            totalWorked += now - clockInTime.Value;

        return (totalWorked, totalBreaks);
    }
}
