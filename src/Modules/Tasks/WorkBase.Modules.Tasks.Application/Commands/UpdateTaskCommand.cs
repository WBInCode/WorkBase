using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record UpdateTaskCommand(
    Guid TaskId, string Title, string? Description,
    Guid PriorityId, DateTime? DueDate) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateTaskHandler(
    ITaskItemRepository taskRepository,
    ITaskPriorityRepository priorityRepository)
    : ICommandHandler<UpdateTaskCommand>
{
    public async Task<Result> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return Result.Failure(Error.NotFound("Task.NotFound",
                $"Zadanie o id '{request.TaskId}' nie zostało znalezione."));

        var priority = await priorityRepository.GetByIdAsync(request.PriorityId, cancellationToken);
        if (priority is null)
            return Result.Failure(Error.NotFound("Task.PriorityNotFound",
                $"Priorytet o id '{request.PriorityId}' nie został znaleziony."));

        task.Update(request.Title, request.Description, request.PriorityId, request.DueDate);
        taskRepository.Update(task);

        return Result.Success();
    }
}
