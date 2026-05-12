using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class DeleteOrgUnitScheduleHandler(
    IOrgUnitScheduleRepository repository,
    IScheduleRepository scheduleRepository)
    : ICommandHandler<DeleteOrgUnitScheduleCommand>
{
    public async Task<Result> Handle(DeleteOrgUnitScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await repository.GetByIdAsync(request.TenantId, request.Id, cancellationToken);
        if (schedule is null)
            return Result.Failure(Error.NotFound("OrgUnitSchedule.NotFound", "Grafik jednostki nie został znaleziony."));

        // Remove future generated schedules from this org unit schedule
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var futureSchedules = await scheduleRepository.GetByOrgUnitScheduleIdAsync(
            request.TenantId, request.Id, today, cancellationToken);

        if (futureSchedules.Count > 0)
            scheduleRepository.RemoveRange(futureSchedules);

        repository.Remove(schedule);

        return Result.Success();
    }
}
