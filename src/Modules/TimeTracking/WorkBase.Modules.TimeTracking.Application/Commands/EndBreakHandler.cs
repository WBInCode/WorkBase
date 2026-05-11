using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class EndBreakHandler(ITimeEntryRepository timeEntryRepository)
    : ICommandHandler<EndBreakCommand, Guid>
{
    public async Task<Result<Guid>> Handle(EndBreakCommand request, CancellationToken cancellationToken)
    {
        // Check only today's entries
        var lastEntry = await timeEntryRepository.GetLastEntryTodayAsync(
            request.TenantId, request.EmployeeId, cancellationToken);

        if (lastEntry is null || lastEntry.Type is TimeEntryType.ClockOut)
            return Result.Failure<Guid>(Error.Conflict(
                "TimeEntry.NotClockedIn",
                "Pracownik nie jest zarejestrowany jako obecny."));

        if (lastEntry.Type is not TimeEntryType.BreakStart)
            return Result.Failure<Guid>(Error.Conflict(
                "TimeEntry.NotOnBreak",
                "Pracownik nie jest na przerwie."));

        var entry = TimeEntry.Create(
            request.TenantId,
            request.EmployeeId,
            DateTime.UtcNow,
            TimeEntryType.BreakEnd,
            ClockMethod.Manual,
            request.Note);

        await timeEntryRepository.AddAsync(entry, cancellationToken);

        return entry.Id;
    }
}
