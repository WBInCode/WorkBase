using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class ClockInHandler(
    ITimeEntryRepository timeEntryRepository,
    IScheduleRepository scheduleRepository)
    : ICommandHandler<ClockInCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ClockInCommand request, CancellationToken cancellationToken)
    {
        // Check only today's entries — previous day state should not block a new day clock-in
        var lastEntry = await timeEntryRepository.GetLastEntryTodayAsync(
            request.TenantId, request.EmployeeId, cancellationToken);

        if (lastEntry is not null && lastEntry.Type is TimeEntryType.ClockIn or TimeEntryType.BreakEnd)
            return Result.Failure<Guid>(Error.Conflict(
                "TimeEntry.AlreadyClockedIn",
                "Pracownik jest już zarejestrowany jako obecny. Najpierw zarejestruj wyjście."));

        if (lastEntry is not null && lastEntry.Type is TimeEntryType.BreakStart)
            return Result.Failure<Guid>(Error.Conflict(
                "TimeEntry.OnBreak",
                "Pracownik jest na przerwie. Najpierw zakończ przerwę."));

        var now = DateTime.UtcNow;

        var entry = TimeEntry.Create(
            request.TenantId,
            request.EmployeeId,
            now,
            TimeEntryType.ClockIn,
            ClockMethod.Manual,
            request.Note,
            request.IpAddress,
            request.Location);

        await timeEntryRepository.AddAsync(entry, cancellationToken);

        // Auto-create Unplanned schedule if no schedule exists for today
        var today = DateOnly.FromDateTime(now);
        var existingSchedule = await scheduleRepository.GetByDateAsync(
            request.TenantId, request.EmployeeId, today, cancellationToken);

        if (existingSchedule is null)
        {
            var clockInTime = TimeOnly.FromDateTime(now);
            var unplannedSchedule = Schedule.Create(
                request.TenantId,
                request.EmployeeId,
                today,
                clockInTime,
                clockInTime, // PlannedEnd will be updated at clock-out
                shiftType: "nieplanowana",
                source: ScheduleSource.Unplanned);

            await scheduleRepository.AddAsync(unplannedSchedule, cancellationToken);
        }

        return entry.Id;
    }
}
