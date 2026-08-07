using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Services;
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

        var bladCzasu = TimeEntryRules.ValidateEntryTime(request.EntryTime, DateTime.UtcNow);
        if (bladCzasu is not null)
            return Result.Failure(bladCzasu);

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
        var entries = await timeEntryRepository.GetEntriesAroundDateAsync(
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

        var wynik = WorkedTimeCalculator.ForDate(entries, date, DateTime.UtcNow);

        if (timeSheet is null)
        {
            timeSheet = TimeSheet.Create(tenantId, employeeId, date);
            timeSheet.Recalculate(wynik.Worked, wynik.Breaks);
            await timeSheetRepository.AddAsync(timeSheet, cancellationToken);
        }
        else
        {
            timeSheet.Recalculate(wynik.Worked, wynik.Breaks);
            timeSheetRepository.Update(timeSheet);
        }
    }
}
