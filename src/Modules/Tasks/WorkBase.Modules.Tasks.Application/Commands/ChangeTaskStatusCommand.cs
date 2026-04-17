using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Application.Services;
using WorkBase.Modules.Tasks.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record ChangeTaskStatusCommand(
    Guid TaskId, Guid NewStatusId, Guid ChangedById) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class ChangeTaskStatusHandler(
    ITaskItemRepository taskRepository,
    ITaskStatusMachine statusMachine,
    ITaskStatusRepository statusRepository,
    ITaskHistoryRepository historyRepository)
    : ICommandHandler<ChangeTaskStatusCommand>
{
    public async Task<Result> Handle(ChangeTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return Result.Failure(Error.NotFound("Task.NotFound",
                $"Zadanie o id '{request.TaskId}' nie zostało znalezione."));

        var validationResult = await statusMachine.ValidateTransitionAsync(
            request.TenantId, task.StatusId, request.NewStatusId, cancellationToken);

        if (validationResult.IsFailure)
            return validationResult;

        var oldStatusId = task.StatusId;
        task.ChangeStatus(request.NewStatusId, request.ChangedById);

        // Check if new status is final → mark complete
        var newStatus = await statusRepository.GetByIdAsync(request.NewStatusId, cancellationToken);
        if (newStatus?.IsFinal == true)
            task.Complete(DateTime.UtcNow);

        // Record history
        var history = TaskHistoryEntry.Create(
            request.TenantId, request.TaskId, request.ChangedById,
            "StatusId", oldStatusId.ToString(), request.NewStatusId.ToString());
        await historyRepository.AddAsync(history, cancellationToken);

        taskRepository.Update(task);
        return Result.Success();
    }
}
