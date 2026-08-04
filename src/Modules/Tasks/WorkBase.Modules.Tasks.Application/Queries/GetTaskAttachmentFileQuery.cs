using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Queries;

public sealed record TaskAttachmentFileDto(string FileName, string ContentType, string StoragePath);

public sealed record GetTaskAttachmentFileQuery(Guid TaskId, Guid AttachmentId)
    : IQuery<TaskAttachmentFileDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetTaskAttachmentFileHandler(ITaskAttachmentRepository repository)
    : IQueryHandler<GetTaskAttachmentFileQuery, TaskAttachmentFileDto>
{
    public async Task<Result<TaskAttachmentFileDto>> Handle(
        GetTaskAttachmentFileQuery request, CancellationToken cancellationToken)
    {
        var attachment = await repository.GetByIdAsync(request.AttachmentId, cancellationToken);
        if (attachment is null
            || attachment.TaskId != request.TaskId
            || attachment.TenantId != request.TenantId)
        {
            return Result.Failure<TaskAttachmentFileDto>(
                Error.NotFound("Task.AttachmentNotFound", "Załącznik nie został znaleziony."));
        }

        return new TaskAttachmentFileDto(attachment.FileName, attachment.ContentType, attachment.StoragePath);
    }
}
