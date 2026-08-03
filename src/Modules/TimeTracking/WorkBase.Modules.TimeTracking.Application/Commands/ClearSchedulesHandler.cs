using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class ClearSchedulesHandler(IScheduleRepository scheduleRepository)
    : ICommandHandler<ClearSchedulesCommand, int>
{
    public async Task<Result<int>> Handle(ClearSchedulesCommand request, CancellationToken cancellationToken)
    {
        if (request.EmployeeIds.Count == 0)
            return Result.Failure<int>(Error.Validation("Schedule.NoEmployees", "Nie wybrano pracowników."));

        if (request.From > request.To)
            return Result.Failure<int>(Error.Validation("Schedule.InvalidRange", "Data początkowa jest po dacie końcowej."));

        var existing = await scheduleRepository.GetByEmployeesDateRangeAsync(
            request.TenantId, request.EmployeeIds, request.From, request.To, cancellationToken);

        // Wpisy z grafiku jednostki odtwarza cotygodniowe zadanie, więc kasujemy je tylko na
        // wyraźne żądanie — inaczej wracają w poniedziałek i wygląda to jak awaria.
        var toRemove = request.IncludeOrgUnitGenerated
            ? existing
            : existing.Where(schedule => schedule.Source != ScheduleSource.OrgUnit).ToList();

        if (toRemove.Count == 0)
            return 0;

        scheduleRepository.RemoveRange(toRemove);

        return toRemove.Count;
    }
}
