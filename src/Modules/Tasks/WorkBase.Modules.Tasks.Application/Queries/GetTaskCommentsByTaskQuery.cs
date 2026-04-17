using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Queries;

public sealed record GetTaskCommentsByTaskQuery(Guid TaskId) : IQuery<List<TaskCommentDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetTaskCommentsByTaskHandler(ITaskCommentRepository repository)
    : IQueryHandler<GetTaskCommentsByTaskQuery, List<TaskCommentDto>>
{
    public async Task<Result<List<TaskCommentDto>>> Handle(
        GetTaskCommentsByTaskQuery request, CancellationToken cancellationToken)
    {
        var comments = await repository.GetByTaskAsync(request.TenantId, request.TaskId, cancellationToken);
        var dtos = comments
            .Select(c => new TaskCommentDto(c.Id, c.TaskId, c.AuthorId, c.Content, c.CreatedAt))
            .ToList();
        return dtos;
    }
}
