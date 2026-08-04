using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;
using WorkBase.Shared.Security;
using WorkBase.Shared.Storage;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record UploadTaskAttachmentCommand(
    Guid TaskId, string FileName, string ContentType,
    long FileSizeBytes, Stream Content, Guid UploadedById)
    : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UploadTaskAttachmentHandler(
    ITaskItemRepository taskRepository,
    ITaskAttachmentRepository attachmentRepository,
    IFileStorage fileStorage,
    ITenantConfigService tenantConfig,
    IUploadScanGuard scanGuard)
    : ICommandHandler<UploadTaskAttachmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UploadTaskAttachmentCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return Result.Failure<Guid>(Error.NotFound("Task.NotFound",
                $"Zadanie o id '{request.TaskId}' nie zostało znalezione."));

        var settings = await tenantConfig.GetAsync<AttachmentUploadSettings>(
            request.TenantId, AttachmentUploadSettings.TenantConfigKey, cancellationToken)
            ?? new AttachmentUploadSettings();

        // Path.GetFileName obcina katalogi (w tym „../”) przemycone w nazwie pliku.
        var safeFileName = Path.GetFileName(request.FileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
            return Result.Failure<Guid>(Error.Validation("Task.InvalidFileName", "Nieprawidłowa nazwa pliku."));

        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !settings.AllowedExtensions.Contains(extension))
            return Result.Failure<Guid>(Error.Validation("Task.ExtensionNotAllowed",
                $"Niedozwolone rozszerzenie pliku '{extension}'. Dozwolone: {string.Join(", ", settings.AllowedExtensions)}"));

        if (request.FileSizeBytes > settings.MaxFileSizeBytes)
            return Result.Failure<Guid>(Error.Validation("Task.AttachmentTooLarge",
                $"Plik przekracza maksymalny dozwolony rozmiar ({settings.MaxFileSizeBytes / (1024 * 1024)} MB)."));

        var skan = await scanGuard.InspectAsync(request.Content, safeFileName, cancellationToken);
        if (skan.IsFailure)
            return Result.Failure<Guid>(skan.Error);

        var storagePath = $"tasks/{request.TenantId}/{request.TaskId}/{Guid.NewGuid()}/{safeFileName}";
        await fileStorage.UploadAsync("workbase", storagePath, request.Content, request.ContentType, cancellationToken);

        var attachment = TaskAttachment.Create(
            request.TenantId, request.TaskId, safeFileName, storagePath,
            request.ContentType, request.FileSizeBytes, request.UploadedById);

        await attachmentRepository.AddAsync(attachment, cancellationToken);
        return attachment.Id;
    }
}
