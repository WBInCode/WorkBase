using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record AssignTaskCommand(
    Guid TaskId, Guid NewAssigneeId, IReadOnlyList<Guid>? AdditionalAssigneeIds = null) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class AssignTaskHandler(
    ITaskItemRepository taskRepository,
    ITaskHistoryRepository historyRepository)
    : ICommandHandler<AssignTaskCommand>
{
    public async Task<Result> Handle(AssignTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return Result.Failure(Error.NotFound("Task.NotFound",
                $"Zadanie o id '{request.TaskId}' nie zostało znalezione."));

        var oldAssigneeId = task.AssigneeId;
        var oldAdditional = task.AdditionalAssignees.Select(a => a.EmployeeId).OrderBy(x => x).ToList();
        task.Assign(request.NewAssigneeId);
        task.SetAdditionalAssignees(request.AdditionalAssigneeIds ?? Array.Empty<Guid>());

        if (oldAssigneeId != request.NewAssigneeId)
        {
            var history = TaskHistoryEntry.Create(
                request.TenantId, request.TaskId, request.NewAssigneeId,
                "AssigneeId", oldAssigneeId.ToString(), request.NewAssigneeId.ToString());
            await historyRepository.AddAsync(history, cancellationToken);
        }

        var newAdditional = task.AdditionalAssignees.Select(a => a.EmployeeId).OrderBy(x => x).ToList();
        if (!oldAdditional.SequenceEqual(newAdditional))
        {
            var history = TaskHistoryEntry.Create(
                request.TenantId, request.TaskId, request.NewAssigneeId,
                "AdditionalAssignees",
                string.Join(",", oldAdditional),
                string.Join(",", newAdditional));
            await historyRepository.AddAsync(history, cancellationToken);
        }

        taskRepository.Update(task);
        return Result.Success();
    }
}
