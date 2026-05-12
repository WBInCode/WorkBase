using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Events;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class UpdateOrgUnitScheduleHandler(IOrgUnitScheduleRepository repository)
    : ICommandHandler<UpdateOrgUnitScheduleCommand>
{
    public async Task<Result> Handle(UpdateOrgUnitScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await repository.GetByIdAsync(request.TenantId, request.Id, cancellationToken);
        if (schedule is null)
            return Result.Failure(Error.NotFound("OrgUnitSchedule.NotFound", "Grafik jednostki nie został znaleziony."));

        schedule.Update(request.Name, request.WeekPattern, request.EffectiveFrom);
        schedule.RaiseDomainEvent(new OrgUnitScheduleChangedEvent(schedule.Id, request.TenantId, schedule.OrgUnitId));

        repository.Update(schedule);

        return Result.Success();
    }
}
