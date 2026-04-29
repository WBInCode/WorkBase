using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Modules.Tasks.Application.Queries;

public sealed record GetTasksQuery(Guid? AssigneeId = null) : IQuery<List<TaskItemDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetTasksHandler(
    ITaskItemRepository taskRepository,
    ITaskStatusRepository statusRepository,
    ITaskPriorityRepository priorityRepository)
    : IQueryHandler<GetTasksQuery, List<TaskItemDto>>
{
    public async Task<Result<List<TaskItemDto>>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var tasks = request.AssigneeId.HasValue
            ? await taskRepository.GetByAssigneeAsync(request.TenantId, request.AssigneeId.Value, cancellationToken)
            : await taskRepository.GetByTenantAsync(request.TenantId, cancellationToken);

        var statuses = await statusRepository.GetByTenantAsync(request.TenantId, cancellationToken);
        var statusMap = statuses.ToDictionary(s => s.Id);

        var priorities = await priorityRepository.GetByTenantAsync(request.TenantId, cancellationToken);
        var priorityMap = priorities.ToDictionary(p => p.Id);

        var dtos = tasks.OrderByDescending(t => t.CreatedAt)
            .Select(t =>
            {
                statusMap.TryGetValue(t.StatusId, out var status);
                priorityMap.TryGetValue(t.PriorityId, out var priority);
                return new TaskItemDto(
                    t.Id, t.Title, t.Description,
                    t.StatusId, status?.Name ?? "?", status?.Color,
                    t.PriorityId, priority?.Name ?? "?", priority?.Color,
                    t.AssigneeId, t.ReporterId,
                    t.DueDate, t.CompletedAt, t.CreatedAt,
                    t.CoAssigneeId);
            }).ToList();

        return dtos;
    }
}
