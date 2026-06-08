using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Events;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class CreateOrgUnitScheduleHandler(IOrgUnitScheduleRepository repository)
    : ICommandHandler<CreateOrgUnitScheduleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrgUnitScheduleCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByOrgUnitIdAsync(request.TenantId, request.OrgUnitId, cancellationToken);
        if (existing is not null)
            return Result.Failure<Guid>(Error.Conflict(
                "OrgUnitSchedule.AlreadyExists",
                "Jednostka organizacyjna ma już przypisany aktywny grafik."));

        var schedule = OrgUnitSchedule.Create(
            request.TenantId,
            request.OrgUnitId,
            request.Name,
            request.WeekPattern,
            request.EffectiveFrom);

        schedule.RaiseDomainEvent(new OrgUnitScheduleChangedEvent(schedule.Id, request.TenantId, request.OrgUnitId));

        await repository.AddAsync(schedule, cancellationToken);

        return schedule.Id;
    }
}
