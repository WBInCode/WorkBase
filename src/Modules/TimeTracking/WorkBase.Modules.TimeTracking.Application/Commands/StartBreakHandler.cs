using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class StartBreakHandler(ITimeEntryRepository timeEntryRepository)
    : ICommandHandler<StartBreakCommand, Guid>
{
    public async Task<Result<Guid>> Handle(StartBreakCommand request, CancellationToken cancellationToken)
    {
        var lastEntry = await timeEntryRepository.GetLastEntryAsync(
            request.TenantId, request.EmployeeId, cancellationToken);

        if (lastEntry is null || lastEntry.Type is TimeEntryType.ClockOut)
            return Result.Failure<Guid>(Error.Conflict(
                "TimeEntry.NotClockedIn",
                "Pracownik nie jest zarejestrowany jako obecny. Najpierw zarejestruj wejście."));

        if (lastEntry.Type is TimeEntryType.BreakStart)
            return Result.Failure<Guid>(Error.Conflict(
                "TimeEntry.AlreadyOnBreak",
                "Pracownik jest już na przerwie."));

        var entry = TimeEntry.Create(
            request.TenantId,
            request.EmployeeId,
            DateTime.UtcNow,
            TimeEntryType.BreakStart,
            ClockMethod.Manual,
            request.Note);

        await timeEntryRepository.AddAsync(entry, cancellationToken);

        return entry.Id;
    }
}
