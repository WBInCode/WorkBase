using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record CreateTaskCommand(
    string Title, string? Description,
    Guid PriorityId, Guid AssigneeId,
    Guid? ReporterId = null, DateTime? DueDate = null,
    IReadOnlyList<Guid>? AdditionalAssigneeIds = null) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateTaskHandler(
    ITaskItemRepository taskRepository,
    ITaskStatusRepository statusRepository,
    ITaskPriorityRepository priorityRepository)
    : ICommandHandler<CreateTaskCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var priority = await priorityRepository.GetByIdAsync(request.PriorityId, cancellationToken);
        if (priority is null)
            return Result.Failure<Guid>(Error.NotFound("Task.PriorityNotFound",
                $"Priorytet o id '{request.PriorityId}' nie został znaleziony."));

        var defaultStatus = await statusRepository.GetDefaultAsync(request.TenantId, cancellationToken);
        if (defaultStatus is null)
            return Result.Failure<Guid>(new Error("Task.NoDefaultStatus",
                "Brak domyślnego statusu zadania. Skontaktuj się z administratorem."));

        var task = TaskItem.Create(
            request.TenantId, request.Title, defaultStatus.Id, request.PriorityId,
            request.AssigneeId, request.ReporterId, request.Description, request.DueDate,
            request.AdditionalAssigneeIds);

        await taskRepository.AddAsync(task, cancellationToken);
        return task.Id;
    }
}
