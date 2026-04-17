using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record AddTaskAttachmentCommand(
    Guid TaskId, string FileName, string StoragePath,
    string ContentType, long FileSizeBytes,
    Guid UploadedById) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class AddTaskAttachmentHandler(
    ITaskItemRepository taskRepository,
    ITaskAttachmentRepository attachmentRepository)
    : ICommandHandler<AddTaskAttachmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddTaskAttachmentCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return Result.Failure<Guid>(Error.NotFound("Task.NotFound",
                $"Zadanie o id '{request.TaskId}' nie zostało znalezione."));

        var attachment = TaskAttachment.Create(
            request.TenantId, request.TaskId, request.FileName,
            request.StoragePath, request.ContentType,
            request.FileSizeBytes, request.UploadedById);

        await attachmentRepository.AddAsync(attachment, cancellationToken);
        return attachment.Id;
    }
}
