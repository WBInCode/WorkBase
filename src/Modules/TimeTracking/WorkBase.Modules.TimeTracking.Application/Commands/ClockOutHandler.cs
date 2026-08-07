using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Services;
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
        var entries = await timeEntryRepository.GetEntriesAroundDateAsync(
            request.TenantId, request.EmployeeId, today, cancellationToken);
        entries.Add(entry);

        var wynik = WorkedTimeCalculator.ForDate(entries, today, now);

        var timeSheet = await timeSheetRepository.GetByDateAsync(
            request.TenantId, request.EmployeeId, today, cancellationToken);

        if (timeSheet is null)
        {
            timeSheet = TimeSheet.Create(request.TenantId, request.EmployeeId, today);
            timeSheet.Recalculate(wynik.Worked, wynik.Breaks);
            await timeSheetRepository.AddAsync(timeSheet, cancellationToken);
        }
        else
        {
            timeSheet.Recalculate(wynik.Worked, wynik.Breaks);
            timeSheetRepository.Update(timeSheet);
        }

        // Zmiana nocna: wejście było wczoraj, więc tamta doba też wymaga przeliczenia.
        var wczoraj = today.AddDays(-1);
        if (entries.Any(e => DateOnly.FromDateTime(e.EntryTime) == wczoraj))
        {
            var wynikWczoraj = WorkedTimeCalculator.ForDate(entries, wczoraj, now);
            var kartaWczoraj = await timeSheetRepository.GetByDateAsync(
                request.TenantId, request.EmployeeId, wczoraj, cancellationToken);

            if (kartaWczoraj is null)
            {
                kartaWczoraj = TimeSheet.Create(request.TenantId, request.EmployeeId, wczoraj);
                kartaWczoraj.Recalculate(wynikWczoraj.Worked, wynikWczoraj.Breaks);
                await timeSheetRepository.AddAsync(kartaWczoraj, cancellationToken);
            }
            else
            {
                kartaWczoraj.Recalculate(wynikWczoraj.Worked, wynikWczoraj.Breaks);
                timeSheetRepository.Update(kartaWczoraj);
            }
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
}
