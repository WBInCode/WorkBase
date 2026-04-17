using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Queries;

public sealed record GetTaskAttachmentsByTaskQuery(Guid TaskId) : IQuery<List<TaskAttachmentDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetTaskAttachmentsByTaskHandler(ITaskAttachmentRepository repository)
    : IQueryHandler<GetTaskAttachmentsByTaskQuery, List<TaskAttachmentDto>>
{
    public async Task<Result<List<TaskAttachmentDto>>> Handle(
        GetTaskAttachmentsByTaskQuery request, CancellationToken cancellationToken)
    {
        var attachments = await repository.GetByTaskAsync(request.TenantId, request.TaskId, cancellationToken);
        var dtos = attachments
            .Select(a => new TaskAttachmentDto(
                a.Id, a.TaskId, a.FileName, a.ContentType,
                a.FileSizeBytes, a.UploadedById, a.UploadedAt))
            .ToList();
        return dtos;
    }
}
