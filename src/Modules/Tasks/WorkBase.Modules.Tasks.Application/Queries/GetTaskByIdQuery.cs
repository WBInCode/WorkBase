using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Modules.Tasks.Application.Queries;

public sealed record GetTaskByIdQuery(Guid Id) : IQuery<TaskItemDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetTaskByIdHandler(
    ITaskItemRepository taskRepository,
    ITaskStatusRepository statusRepository,
    ITaskPriorityRepository priorityRepository)
    : IQueryHandler<GetTaskByIdQuery, TaskItemDto>
{
    public async Task<Result<TaskItemDto>> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.Id, cancellationToken);
        if (task is null || task.TenantId != request.TenantId)
            return Result.Failure<TaskItemDto>(Error.NotFound("Task.NotFound", "Zadanie nie zostało znalezione."));

        var statuses = await statusRepository.GetByTenantAsync(request.TenantId, cancellationToken);
        var statusMap = statuses.ToDictionary(s => s.Id);

        var priorities = await priorityRepository.GetByTenantAsync(request.TenantId, cancellationToken);
        var priorityMap = priorities.ToDictionary(p => p.Id);

        statusMap.TryGetValue(task.StatusId, out var status);
        priorityMap.TryGetValue(task.PriorityId, out var priority);

        return new TaskItemDto(
            task.Id, task.Title, task.Description,
            task.StatusId, status?.Name ?? "?", status?.Color,
            task.PriorityId, priority?.Name ?? "?", priority?.Color,
            task.AssigneeId, task.ReporterId,
            task.DueDate, task.CompletedAt, task.CreatedAt,
            task.AdditionalAssignees.Select(a => a.EmployeeId).ToList());
    }
}
