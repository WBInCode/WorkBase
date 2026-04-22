using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class AdminCreateTimeEntryHandler(ITimeEntryRepository timeEntryRepository)
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

        return entry.Id;
    }
}
