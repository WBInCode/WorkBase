using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Documents.Domain.Entities;

public sealed class Document : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public Guid UploadedById { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? Description { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Document() { }

    public static Document Create(
        Guid tenantId, string fileName, string storagePath, string contentType,
        long fileSizeBytes, Guid uploadedById, Guid? categoryId = null,
        string? entityType = null, Guid? entityId = null, string? description = null)
    {
        return new Document
        {
            TenantId = tenantId,
            FileName = fileName,
            StoragePath = storagePath,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadedById = uploadedById,
            CategoryId = categoryId,
            EntityType = entityType,
            EntityId = entityId,
            Description = description
        };
    }

    public void SoftDelete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void UpdateCategory(Guid? categoryId) => CategoryId = categoryId;
}
