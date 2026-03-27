using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed class GetSchedulesHandler(IScheduleRepository scheduleRepository)
    : IQueryHandler<GetSchedulesQuery, IReadOnlyList<ScheduleDto>>
{
    public async Task<Result<IReadOnlyList<ScheduleDto>>> Handle(
        GetSchedulesQuery request,
        CancellationToken cancellationToken)
    {
        var schedules = await scheduleRepository.GetByDateRangeAsync(
            request.TenantId, request.EmployeeId, request.From, request.To, cancellationToken);

        var dtos = schedules.Select(s => new ScheduleDto(
            s.Id,
            s.EmployeeId,
            s.Date,
            s.PlannedStart,
            s.PlannedEnd,
            s.ShiftType,
            s.TemplateId,
            s.PlannedDuration)).ToList();

        return dtos;
    }
}
