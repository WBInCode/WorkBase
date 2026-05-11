using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class AdminUpdateTimeEntryHandler(
    ITimeEntryRepository timeEntryRepository,
    ITimeSheetRepository timeSheetRepository)
    : ICommandHandler<AdminUpdateTimeEntryCommand>
{
    public async Task<Result> Handle(AdminUpdateTimeEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await timeEntryRepository.GetByIdAsync(request.TenantId, request.EntryId, cancellationToken);
        if (entry is null)
            return Result.Failure(Error.NotFound("TimeEntry.NotFound", "Wpis nie został znaleziony."));

        if (!Enum.TryParse<TimeEntryType>(request.Type, true, out var entryType))
            return Result.Failure(Error.Validation(
                "TimeEntry.InvalidType",
                "Nieprawidłowy typ wpisu. Dozwolone: ClockIn, ClockOut, BreakStart, BreakEnd."));

        BreakType? breakType = null;
        if (!string.IsNullOrEmpty(request.BreakType))
        {
            if (!Enum.TryParse<BreakType>(request.BreakType, true, out var bt))
                return Result.Failure(Error.Validation(
                    "TimeEntry.InvalidBreakType",
                    "Nieprawidłowy typ przerwy. Dozwolone: Paid, Unpaid."));
            breakType = bt;
        }

        var oldDate = DateOnly.FromDateTime(entry.EntryTime);
        var newDate = DateOnly.FromDateTime(request.EntryTime);

        entry.UpdateEntry(request.EntryTime, entryType, breakType, request.Note);

        // Recalculate timesheet for the new date
        await RecalculateTimeSheet(request.TenantId, entry.EmployeeId, newDate, cancellationToken);

        // If date changed, also recalculate the old date
        if (oldDate != newDate)
            await RecalculateTimeSheet(request.TenantId, entry.EmployeeId, oldDate, cancellationToken);

        return Result.Success();
    }

    private async Task RecalculateTimeSheet(Guid tenantId, Guid employeeId, DateOnly date, CancellationToken cancellationToken)
    {
        var entries = await timeEntryRepository.GetEntriesForDateAsync(
            tenantId, employeeId, date, cancellationToken);

        var timeSheet = await timeSheetRepository.GetByDateAsync(
            tenantId, employeeId, date, cancellationToken);

        if (entries.Count == 0)
        {
            if (timeSheet is not null)
            {
                timeSheet.Recalculate(TimeSpan.Zero, TimeSpan.Zero);
                timeSheet.MarkIncomplete();
                timeSheetRepository.Update(timeSheet);
            }
            return;
        }

        var (totalWorked, totalBreaks) = CalculateWorkedTime(entries);

        if (timeSheet is null)
        {
            timeSheet = TimeSheet.Create(tenantId, employeeId, date);
            timeSheet.Recalculate(totalWorked, totalBreaks);
            await timeSheetRepository.AddAsync(timeSheet, cancellationToken);
        }
        else
        {
            timeSheet.Recalculate(totalWorked, totalBreaks);
            timeSheetRepository.Update(timeSheet);
        }
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

        if (breakStartTime.HasValue)
            totalBreaks += now - breakStartTime.Value;

        if (clockInTime.HasValue)
            totalWorked += now - clockInTime.Value;

        return (totalWorked, totalBreaks);
    }
}
