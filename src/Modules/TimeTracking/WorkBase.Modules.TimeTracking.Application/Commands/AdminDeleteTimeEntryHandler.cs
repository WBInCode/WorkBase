using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Services;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class AdminDeleteTimeEntryHandler(
    ITimeEntryRepository timeEntryRepository,
    ITimeSheetRepository timeSheetRepository)
    : ICommandHandler<AdminDeleteTimeEntryCommand>
{
    public async Task<Result> Handle(AdminDeleteTimeEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await timeEntryRepository.GetByIdAsync(request.TenantId, request.EntryId, cancellationToken);
        if (entry is null)
            return Result.Failure(Error.NotFound("TimeEntry.NotFound", "Wpis nie został znaleziony."));

        var date = DateOnly.FromDateTime(entry.EntryTime);
        var employeeId = entry.EmployeeId;

        timeEntryRepository.Delete(entry);

        // Recalculate timesheet for the affected day
        var entries = await timeEntryRepository.GetEntriesAroundDateAsync(
            request.TenantId, employeeId, date, cancellationToken);

        // Exclude the deleted entry (may still be tracked before SaveChanges)
        entries = entries.Where(e => e.Id != request.EntryId).ToList();

        var timeSheet = await timeSheetRepository.GetByDateAsync(
            request.TenantId, employeeId, date, cancellationToken);

        if (entries.Count == 0)
        {
            if (timeSheet is not null)
            {
                timeSheet.Recalculate(TimeSpan.Zero, TimeSpan.Zero);
                timeSheet.MarkIncomplete();
                timeSheetRepository.Update(timeSheet);
            }
        }
        else
        {
            var wynik = WorkedTimeCalculator.ForDate(entries, date, DateTime.UtcNow);

            if (timeSheet is null)
            {
                timeSheet = TimeSheet.Create(request.TenantId, employeeId, date);
                timeSheet.Recalculate(wynik.Worked, wynik.Breaks);
                await timeSheetRepository.AddAsync(timeSheet, cancellationToken);
            }
            else
            {
                timeSheet.Recalculate(wynik.Worked, wynik.Breaks);
                timeSheetRepository.Update(timeSheet);
            }
        }

        return Result.Success();
    }
}
