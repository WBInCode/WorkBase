using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class DeleteScheduleHandler(IScheduleRepository scheduleRepository)
    : ICommandHandler<DeleteScheduleCommand>
{
    public async Task<Result> Handle(DeleteScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await scheduleRepository.GetByIdAsync(
            request.TenantId, request.ScheduleId, cancellationToken);

        if (schedule is null)
            return Result.Failure(Error.NotFound(
                "Schedule.NotFound",
                "Grafik nie został znaleziony."));

        scheduleRepository.Remove(schedule);

        return Result.Success();
    }
}
