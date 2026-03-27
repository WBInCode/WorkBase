using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed class GetCurrentStatusHandler(ITimeEntryRepository timeEntryRepository)
    : IQueryHandler<GetCurrentStatusQuery, TimeStatusDto>
{
    public async Task<Result<TimeStatusDto>> Handle(
        GetCurrentStatusQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var entries = await timeEntryRepository.GetEntriesForDateAsync(
            request.TenantId, request.EmployeeId, today, cancellationToken);

        var lastEntry = entries.OrderByDescending(e => e.EntryTime).FirstOrDefault();

        var status = lastEntry?.Type switch
        {
            TimeEntryType.ClockIn => "working",
            TimeEntryType.BreakStart => "on-break",
            TimeEntryType.BreakEnd => "working",
            TimeEntryType.ClockOut => "ended",
            _ => "not-started"
        };

        var (worked, breaks) = CalculateRunningTime(entries);

        return new TimeStatusDto(
            status,
            lastEntry?.EntryTime,
            lastEntry?.Type.ToString(),
            worked,
            breaks);
    }

    private static (TimeSpan Worked, TimeSpan Breaks) CalculateRunningTime(List<TimeEntry> entries)
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

        // If still clocked in, count time until now
        if (clockInTime.HasValue)
            totalWorked += now - clockInTime.Value;

        // If still on break, count break time until now
        if (breakStartTime.HasValue)
            totalBreaks += now - breakStartTime.Value;

        return (totalWorked, totalBreaks);
    }
}
