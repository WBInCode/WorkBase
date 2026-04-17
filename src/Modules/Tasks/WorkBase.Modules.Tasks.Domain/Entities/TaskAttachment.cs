using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Domain.Entities;

public sealed class TaskAttachment : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid TaskId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string StoragePath { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long FileSizeBytes { get; private set; }
    public Guid UploadedById { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private TaskAttachment() { }

    public static TaskAttachment Create(
        Guid tenantId, Guid taskId, string fileName, string storagePath,
        string contentType, long fileSizeBytes, Guid uploadedById)
    {
        return new TaskAttachment
        {
            TenantId = tenantId,
            TaskId = taskId,
            FileName = fileName,
            StoragePath = storagePath,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadedById = uploadedById,
            UploadedAt = DateTime.UtcNow,
        };
    }
}
