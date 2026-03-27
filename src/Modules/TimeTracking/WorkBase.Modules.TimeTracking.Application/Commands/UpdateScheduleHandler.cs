using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class UpdateScheduleHandler(IScheduleRepository scheduleRepository)
    : ICommandHandler<UpdateScheduleCommand>
{
    public async Task<Result> Handle(UpdateScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await scheduleRepository.GetByIdAsync(
            request.TenantId, request.ScheduleId, cancellationToken);

        if (schedule is null)
            return Result.Failure(Error.NotFound(
                "Schedule.NotFound",
                "Grafik nie został znaleziony."));

        schedule.Update(request.PlannedStart, request.PlannedEnd, request.ShiftType);
        scheduleRepository.Update(schedule);

        return Result.Success();
    }
}
