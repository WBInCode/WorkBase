using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
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
        var entries = await timeEntryRepository.GetEntriesForDateAsync(
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
            var (totalWorked, totalBreaks) = CalculateWorkedTime(entries);

            if (timeSheet is null)
            {
                timeSheet = TimeSheet.Create(request.TenantId, employeeId, date);
                timeSheet.Recalculate(totalWorked, totalBreaks);
                await timeSheetRepository.AddAsync(timeSheet, cancellationToken);
            }
            else
            {
                timeSheet.Recalculate(totalWorked, totalBreaks);
                timeSheetRepository.Update(timeSheet);
            }
        }

        return Result.Success();
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
