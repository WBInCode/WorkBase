using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Queries;

public sealed record GetTaskStatusesQuery : IQuery<List<TaskStatusDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetTaskStatusesHandler(ITaskStatusRepository statusRepository)
    : IQueryHandler<GetTaskStatusesQuery, List<TaskStatusDto>>
{
    public async Task<Result<List<TaskStatusDto>>> Handle(GetTaskStatusesQuery request, CancellationToken cancellationToken)
    {
        var statuses = await statusRepository.GetByTenantAsync(request.TenantId, cancellationToken);
        var dtos = statuses.OrderBy(s => s.SortOrder)
            .Select(s => new TaskStatusDto(
                s.Id, s.Code, s.Name, s.Color,
                s.IsFinal, s.IsDefault, s.SortOrder))
            .ToList();
        return dtos;
    }
}
