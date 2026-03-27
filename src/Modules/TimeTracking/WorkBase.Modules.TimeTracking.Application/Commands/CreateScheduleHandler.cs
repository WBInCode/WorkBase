using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class CreateScheduleHandler(IScheduleRepository scheduleRepository)
    : ICommandHandler<CreateScheduleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateScheduleCommand request, CancellationToken cancellationToken)
    {
        var existing = await scheduleRepository.GetByDateAsync(
            request.TenantId, request.EmployeeId, request.Date, cancellationToken);

        if (existing is not null)
            return Result.Failure<Guid>(Error.Conflict(
                "Schedule.AlreadyExists",
                $"Grafik dla pracownika na dzień {request.Date} już istnieje."));

        var schedule = Schedule.Create(
            request.TenantId,
            request.EmployeeId,
            request.Date,
            request.PlannedStart,
            request.PlannedEnd,
            request.ShiftType,
            request.TemplateId);

        await scheduleRepository.AddAsync(schedule, cancellationToken);

        return schedule.Id;
    }
}
