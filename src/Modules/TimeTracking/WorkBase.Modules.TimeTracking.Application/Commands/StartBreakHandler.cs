using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class StartBreakHandler(
    ITimeEntryRepository timeEntryRepository,
    IBreakPolicyRepository breakPolicyRepository)
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

        // Validate against break policy if one exists
        var policy = await breakPolicyRepository.GetByTypeAsync(
            request.TenantId, request.BreakType, cancellationToken);

        if (policy is not null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var todayEntries = await timeEntryRepository.GetEntriesForDateAsync(
                request.TenantId, request.EmployeeId, today, cancellationToken);

            var breakCount = todayEntries
                .Count(e => e.Type == TimeEntryType.BreakStart && e.BreakType == request.BreakType);

            if (policy.MaxPerDay.HasValue && breakCount >= policy.MaxPerDay.Value)
                return Result.Failure<Guid>(Error.Conflict(
                    "TimeEntry.BreakLimitReached",
                    $"Osiągnięto limit {policy.MaxPerDay.Value} przerw tego typu na dzień."));

            var totalBreakMinutes = CalculateBreakMinutes(todayEntries, request.BreakType);
            if (policy.MaxMinutesPerDay.HasValue && totalBreakMinutes >= policy.MaxMinutesPerDay.Value)
                return Result.Failure<Guid>(Error.Conflict(
                    "TimeEntry.BreakTimeLimitReached",
                    $"Osiągnięto limit {policy.MaxMinutesPerDay.Value} minut przerw tego typu na dzień."));
        }

        var entry = TimeEntry.Create(
            request.TenantId,
            request.EmployeeId,
            DateTime.UtcNow,
            TimeEntryType.BreakStart,
            ClockMethod.Manual,
            request.Note,
            breakType: request.BreakType);

        await timeEntryRepository.AddAsync(entry, cancellationToken);

        return entry.Id;
    }

    private static double CalculateBreakMinutes(List<TimeEntry> entries, BreakType breakType)
    {
        var ordered = entries.OrderBy(e => e.EntryTime).ToList();
        var total = TimeSpan.Zero;
        DateTime? breakStart = null;

        foreach (var e in ordered)
        {
            if (e.Type == TimeEntryType.BreakStart && e.BreakType == breakType)
                breakStart = e.EntryTime;
            else if (e.Type == TimeEntryType.BreakEnd && breakStart.HasValue)
            {
                total += e.EntryTime - breakStart.Value;
                breakStart = null;
            }
        }

        if (breakStart.HasValue)
            total += DateTime.UtcNow - breakStart.Value;

        return total.TotalMinutes;
    }
}
