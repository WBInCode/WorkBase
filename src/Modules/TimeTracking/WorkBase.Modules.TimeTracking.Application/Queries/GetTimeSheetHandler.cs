using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Services;
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
            // Liczymy z żywych wpisów zawsze: zapisana karta bywa nieaktualna, a te
            // sprzed poprawki kalkulatora zawierają sumy dłuższe niż doba.
            var entries = await timeEntryRepository.GetEntriesAroundDateAsync(
                request.TenantId, request.EmployeeId, date, cancellationToken);
            var wpisyDnia = entries.Where(e => DateOnly.FromDateTime(e.EntryTime) == date).ToList();
            sheetsByDate.TryGetValue(date, out var sheet);

            if (entries.Count > 0)
            {
                var wynik = WorkedTimeCalculator.ForDate(entries, date, DateTime.UtcNow);
                var entryDtos = MapEntries(wpisyDnia);

                var status = wynik.HasOpenSession
                    ? "incomplete"
                    : sheet is not null && wpisyDnia.Count == 0
                        ? sheet.Status.ToString().ToLowerInvariant()
                        : "complete";

                if (wynik.Worked == TimeSpan.Zero && wpisyDnia.Count == 0)
                {
                    days.Add(new TimeSheetDayDto(date, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, "empty", sheet?.Note, []));
                    continue;
                }

                days.Add(new TimeSheetDayDto(date, wynik.Worked, wynik.Breaks, wynik.Net, status, sheet?.Note, entryDtos));

                totalWorked += wynik.Worked;
                totalBreaks += wynik.Breaks;

                if (status is "complete" or "approved")
                    daysWorked++;
                else
                    daysIncomplete++;
            }
            else
            {
                days.Add(new TimeSheetDayDto(date, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, "empty", sheet?.Note, []));
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
}
