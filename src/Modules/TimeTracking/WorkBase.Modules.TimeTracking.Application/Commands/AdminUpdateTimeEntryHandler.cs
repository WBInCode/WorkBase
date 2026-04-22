using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class AdminUpdateTimeEntryHandler(ITimeEntryRepository timeEntryRepository)
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

        entry.UpdateEntry(request.EntryTime, entryType, breakType, request.Note);

        return Result.Success();
    }
}
