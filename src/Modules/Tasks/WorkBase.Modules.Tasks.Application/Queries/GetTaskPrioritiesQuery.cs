using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Queries;

public sealed record GetTaskPrioritiesQuery : IQuery<List<TaskPriorityDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetTaskPrioritiesHandler(ITaskPriorityRepository priorityRepository)
    : IQueryHandler<GetTaskPrioritiesQuery, List<TaskPriorityDto>>
{
    public async Task<Result<List<TaskPriorityDto>>> Handle(GetTaskPrioritiesQuery request, CancellationToken cancellationToken)
    {
        var priorities = await priorityRepository.GetByTenantAsync(request.TenantId, cancellationToken);
        var dtos = priorities.OrderBy(p => p.SortOrder)
            .Select(p => new TaskPriorityDto(
                p.Id, p.Code, p.Name, p.Color, p.SortOrder))
            .ToList();
        return dtos;
    }
}
