using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed class GetOrgUnitScheduleHandler(IOrgUnitScheduleRepository repository)
    : IQueryHandler<GetOrgUnitScheduleQuery, OrgUnitScheduleDto?>
{
    public async Task<Result<OrgUnitScheduleDto?>> Handle(
        GetOrgUnitScheduleQuery request, CancellationToken cancellationToken)
    {
        var schedule = await repository.GetByOrgUnitIdAsync(request.TenantId, request.OrgUnitId, cancellationToken);
        if (schedule is null)
            return (OrgUnitScheduleDto?)null;

        return new OrgUnitScheduleDto(
            schedule.Id,
            schedule.OrgUnitId,
            schedule.Name,
            schedule.WeekPattern,
            schedule.EffectiveFrom,
            schedule.IsActive);
    }
}

public sealed class GetOrgUnitSchedulesHandler(IOrgUnitScheduleRepository repository)
    : IQueryHandler<GetOrgUnitSchedulesQuery, IReadOnlyList<OrgUnitScheduleDto>>
{
    public async Task<Result<IReadOnlyList<OrgUnitScheduleDto>>> Handle(
        GetOrgUnitSchedulesQuery request, CancellationToken cancellationToken)
    {
        var schedules = await repository.GetAllAsync(request.TenantId, cancellationToken);

        var dtos = schedules.Select(s => new OrgUnitScheduleDto(
            s.Id,
            s.OrgUnitId,
            s.Name,
            s.WeekPattern,
            s.EffectiveFrom,
            s.IsActive)).ToList();

        return dtos;
    }
}
