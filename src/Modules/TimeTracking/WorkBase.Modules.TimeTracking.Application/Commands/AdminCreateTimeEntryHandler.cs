using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Services;
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

        var bladCzasu = TimeEntryRules.ValidateEntryTime(request.EntryTime, DateTime.UtcNow);
        if (bladCzasu is not null)
            return Result.Failure<Guid>(bladCzasu);

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
        var entries = await timeEntryRepository.GetEntriesAroundDateAsync(
            request.TenantId, request.EmployeeId, date, cancellationToken);
        entries.Add(entry);

        var wynik = WorkedTimeCalculator.ForDate(entries, date, DateTime.UtcNow);

        var timeSheet = await timeSheetRepository.GetByDateAsync(
            request.TenantId, request.EmployeeId, date, cancellationToken);

        if (timeSheet is null)
        {
            timeSheet = TimeSheet.Create(request.TenantId, request.EmployeeId, date);
            timeSheet.Recalculate(wynik.Worked, wynik.Breaks);
            await timeSheetRepository.AddAsync(timeSheet, cancellationToken);
        }
        else
        {
            timeSheet.Recalculate(wynik.Worked, wynik.Breaks);
            timeSheetRepository.Update(timeSheet);
        }

        return entry.Id;
    }
}
