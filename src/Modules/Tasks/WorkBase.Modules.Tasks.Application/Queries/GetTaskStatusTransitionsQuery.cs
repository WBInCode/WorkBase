using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Queries;

public sealed record GetTaskStatusTransitionsQuery() : IQuery<List<TaskStatusTransitionDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetTaskStatusTransitionsHandler(ITaskStatusTransitionRepository repository)
    : IQueryHandler<GetTaskStatusTransitionsQuery, List<TaskStatusTransitionDto>>
{
    public async Task<Result<List<TaskStatusTransitionDto>>> Handle(
        GetTaskStatusTransitionsQuery request, CancellationToken cancellationToken)
    {
        var transitions = await repository.GetByTenantAsync(request.TenantId, cancellationToken);
        var dtos = transitions
            .Select(t => new TaskStatusTransitionDto(t.Id, t.FromStatusId, t.ToStatusId))
            .ToList();
        return dtos;
    }
}
