using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class GenerateBatchSchedulesHandler(IScheduleRepository scheduleRepository)
    : ICommandHandler<GenerateBatchSchedulesCommand, int>
{
    public async Task<Result<int>> Handle(GenerateBatchSchedulesCommand request, CancellationToken cancellationToken)
    {
        if (request.EmployeeIds.Count == 0)
            return Result.Failure<int>(Error.Validation("Schedule.NoEmployees", "Nie wybrano pracowników."));

        if (request.WeekPattern.Count == 0)
            return Result.Failure<int>(Error.Validation("Schedule.NoPattern", "Wzorzec tygodnia jest pusty."));

        if (request.From > request.To)
            return Result.Failure<int>(Error.Validation("Schedule.InvalidRange", "Data początkowa jest po dacie końcowej."));

        // Build a lookup: DayOfWeek -> pattern
        var patternByDay = request.WeekPattern
            .GroupBy(p => p.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.First());

        // Fetch existing schedules in the range for all employees
        var existing = await scheduleRepository.GetByEmployeesDateRangeAsync(
            request.TenantId, request.EmployeeIds, request.From, request.To, cancellationToken);

        var existingKeys = existing
            .Select(s => (s.EmployeeId, s.Date))
            .ToHashSet();

        // If overwrite, remove conflicting existing schedules
        if (request.Overwrite)
        {
            var toRemove = existing.Where(s =>
                patternByDay.ContainsKey(s.Date.DayOfWeek)).ToList();

            if (toRemove.Count > 0)
            {
                scheduleRepository.RemoveRange(toRemove);
                foreach (var r in toRemove)
                    existingKeys.Remove((r.EmployeeId, r.Date));
            }
        }

        // Generate schedules
        var newSchedules = new List<Schedule>();
        var current = request.From;

        while (current <= request.To)
        {
            if (patternByDay.TryGetValue(current.DayOfWeek, out var pattern))
            {
                foreach (var employeeId in request.EmployeeIds)
                {
                    if (!existingKeys.Contains((employeeId, current)))
                    {
                        newSchedules.Add(Schedule.Create(
                            request.TenantId,
                            employeeId,
                            current,
                            pattern.PlannedStart,
                            pattern.PlannedEnd,
                            pattern.ShiftType,
                            pattern.TemplateId));
                    }
                }
            }

            current = current.AddDays(1);
        }

        if (newSchedules.Count > 0)
            await scheduleRepository.AddManyAsync(newSchedules, cancellationToken);

        return newSchedules.Count;
    }
}
