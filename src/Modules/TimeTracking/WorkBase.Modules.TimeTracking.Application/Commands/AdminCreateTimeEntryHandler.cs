using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class AdminCreateTimeEntryHandler(
    ITimeEntryRepository timeEntryRepository,
    ITimeSheetRepository timeSheetRepository)
    : ICommandHandler<AdminCreateTimeEntryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AdminCreateTimeEntryCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TimeEntryType>(request.Type, true, out var entryType))
            return Result.Failure<Guid>(Error.Validation(
                "TimeEntry.InvalidType",
                "Nieprawidłowy typ wpisu. Dozwolone: ClockIn, ClockOut, BreakStart, BreakEnd."));

        BreakType? breakType = null;
        if (!string.IsNullOrEmpty(request.BreakType))
        {
            if (!Enum.TryParse<BreakType>(request.BreakType, true, out var bt))
                return Result.Failure<Guid>(Error.Validation(
                    "TimeEntry.InvalidBreakType",
                    "Nieprawidłowy typ przerwy. Dozwolone: Paid, Unpaid."));
            breakType = bt;
        }

        var entry = TimeEntry.Create(
            request.TenantId,
            request.EmployeeId,
            request.EntryTime,
            entryType,
            ClockMethod.Manual,
            request.Note,
            breakType: breakType);

        await timeEntryRepository.AddAsync(entry, cancellationToken);

        // Recalculate timesheet for the affected day
        var date = DateOnly.FromDateTime(request.EntryTime);
        var entries = await timeEntryRepository.GetEntriesForDateAsync(
            request.TenantId, request.EmployeeId, date, cancellationToken);

        var (totalWorked, totalBreaks) = CalculateWorkedTime(entries);

        var timeSheet = await timeSheetRepository.GetByDateAsync(
            request.TenantId, request.EmployeeId, date, cancellationToken);

        if (timeSheet is null)
        {
            timeSheet = TimeSheet.Create(request.TenantId, request.EmployeeId, date);
            timeSheet.Recalculate(totalWorked, totalBreaks);
            await timeSheetRepository.AddAsync(timeSheet, cancellationToken);
        }
        else
        {
            timeSheet.Recalculate(totalWorked, totalBreaks);
            timeSheetRepository.Update(timeSheet);
        }

        return entry.Id;
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
