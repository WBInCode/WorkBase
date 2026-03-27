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

        for (var date = request.From; date <= request.To; date = date.AddDays(1))
        {
            if (sheetsByDate.TryGetValue(date, out var sheet))
            {
                days.Add(new TimeSheetDayDto(
                    sheet.Date,
                    sheet.TotalWorked,
                    sheet.TotalBreaks,
                    sheet.NetWorked,
                    sheet.Status.ToString().ToLowerInvariant(),
                    sheet.Note));

                totalWorked += sheet.TotalWorked;
                totalBreaks += sheet.TotalBreaks;

                if (sheet.Status == TimeSheetStatus.Complete || sheet.Status == TimeSheetStatus.Approved)
                    daysWorked++;
                else
                    daysIncomplete++;
            }
            else
            {
                // On-demand calculation for days without a persisted timesheet
                var entries = await timeEntryRepository.GetEntriesForDateAsync(
                    request.TenantId, request.EmployeeId, date, cancellationToken);

                if (entries.Count > 0)
                {
                    var (worked, breaks) = CalculateWorkedTime(entries);

                    var hasClockOut = entries.Any(e => e.Type == TimeEntryType.ClockOut);
                    var status = hasClockOut ? "complete" : "incomplete";

                    days.Add(new TimeSheetDayDto(date, worked, breaks, worked - breaks, status, null));

                    totalWorked += worked;
                    totalBreaks += breaks;

                    if (hasClockOut)
                        daysWorked++;
                    else
                        daysIncomplete++;
                }
                else
                {
                    days.Add(new TimeSheetDayDto(date, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, "empty", null));
                }
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
                    if (clockInTime.HasValue)
                    {
                        totalWorked += entry.EntryTime - clockInTime.Value;
                        clockInTime = null;
                    }
                    breakStartTime = entry.EntryTime;
                    break;

                case TimeEntryType.BreakEnd:
                    if (breakStartTime.HasValue)
                    {
                        totalBreaks += entry.EntryTime - breakStartTime.Value;
                        breakStartTime = null;
                    }
                    clockInTime = entry.EntryTime;
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

        // If still working (no clock-out yet), count up to now
        if (clockInTime.HasValue)
            totalWorked += now - clockInTime.Value;

        if (breakStartTime.HasValue)
            totalBreaks += now - breakStartTime.Value;

        return (totalWorked, totalBreaks);
    }
}
