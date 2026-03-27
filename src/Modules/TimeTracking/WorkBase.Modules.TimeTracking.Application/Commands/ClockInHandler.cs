using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class ClockInHandler(ITimeEntryRepository timeEntryRepository)
    : ICommandHandler<ClockInCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ClockInCommand request, CancellationToken cancellationToken)
    {
        var lastEntry = await timeEntryRepository.GetLastEntryAsync(
            request.TenantId, request.EmployeeId, cancellationToken);

        if (lastEntry is not null && lastEntry.Type is TimeEntryType.ClockIn or TimeEntryType.BreakEnd)
            return Result.Failure<Guid>(Error.Conflict(
                "TimeEntry.AlreadyClockedIn",
                "Pracownik jest już zarejestrowany jako obecny. Najpierw zarejestruj wyjście."));

        if (lastEntry is not null && lastEntry.Type is TimeEntryType.BreakStart)
            return Result.Failure<Guid>(Error.Conflict(
                "TimeEntry.OnBreak",
                "Pracownik jest na przerwie. Najpierw zakończ przerwę."));

        var entry = TimeEntry.Create(
            request.TenantId,
            request.EmployeeId,
            DateTime.UtcNow,
            TimeEntryType.ClockIn,
            ClockMethod.Manual,
            request.Note,
            request.IpAddress,
            request.Location);

        await timeEntryRepository.AddAsync(entry, cancellationToken);

        return entry.Id;
    }
}
