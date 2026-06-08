using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class ClockOutHandler(
    ITimeEntryRepository timeEntryRepository,
    ITimeSheetRepository timeSheetRepository,
    IScheduleRepository scheduleRepository)
    : ICommandHandler<ClockOutCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ClockOutCommand request, CancellationToken cancellationToken)
    {
        // Check only today's entries — previous day state should not affect today
        var lastEntry = await timeEntryRepository.GetLastEntryTodayAsync(
            request.TenantId, request.EmployeeId, cancellationToken);

        if (lastEntry is null || lastEntry.Type is TimeEntryType.ClockOut)
            return Result.Failure<Guid>(Error.Conflict(
                "TimeEntry.NotClockedIn",
                "Pracownik nie jest zarejestrowany jako obecny. Najpierw zarejestruj wejście."));

        if (lastEntry.Type is TimeEntryType.BreakStart)
            return Result.Failure<Guid>(Error.Conflict(
                "TimeEntry.OnBreak",
                "Pracownik jest na przerwie. Najpierw zakończ przerwę przed rejestracją wyjścia."));

        var now = DateTime.UtcNow;

        var entry = TimeEntry.Create(
            request.TenantId,
            request.EmployeeId,
            now,
            TimeEntryType.ClockOut,
            ClockMethod.Manual,
            request.Note,
            request.IpAddress,
            request.Location);

        await timeEntryRepository.AddAsync(entry, cancellationToken);

        // Recalculate timesheet for today
        var today = DateOnly.FromDateTime(now);
        var entries = await timeEntryRepository.GetEntriesForDateAsync(
            request.TenantId, request.EmployeeId, today, cancellationToken);

        var (totalWorked, totalBreaks) = CalculateWorkedTime(entries);

        var timeSheet = await timeSheetRepository.GetByDateAsync(
            request.TenantId, request.EmployeeId, today, cancellationToken);

        if (timeSheet is null)
        {
            timeSheet = TimeSheet.Create(request.TenantId, request.EmployeeId, today);
            timeSheet.Recalculate(totalWorked, totalBreaks);
            await timeSheetRepository.AddAsync(timeSheet, cancellationToken);
        }
        else
        {
            timeSheet.Recalculate(totalWorked, totalBreaks);
            timeSheetRepository.Update(timeSheet);
        }

        // Update PlannedEnd on Unplanned schedules
        var schedule = await scheduleRepository.GetByDateAsync(
            request.TenantId, request.EmployeeId, today, cancellationToken);

        if (schedule is not null && schedule.Source == ScheduleSource.Unplanned)
        {
            schedule.UpdatePlannedEnd(TimeOnly.FromDateTime(now));
            scheduleRepository.Update(schedule);
        }

        return entry.Id;
    }

    private static (TimeSpan TotalWorked, TimeSpan TotalBreaks) CalculateWorkedTime(List<TimeEntry> entries)
    {
        var ordered = entries.OrderBy(e => e.EntryTime).ToList();

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

        return (totalWorked, totalBreaks);
    }
}
