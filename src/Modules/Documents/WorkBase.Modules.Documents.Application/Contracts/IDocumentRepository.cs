using WorkBase.Modules.Documents.Domain.Entities;

namespace WorkBase.Modules.Documents.Application.Contracts;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Document>> GetByTenantAsync(Guid tenantId, Guid? categoryId, string? entityType, Guid? entityId, bool includeDeleted, CancellationToken ct = default);
    Task<List<Document>> GetByUploadedByAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    Task AddAsync(Document document, CancellationToken ct = default);
    void Update(Document document);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IDocumentCategoryRepository
{
    Task<DocumentCategory?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<DocumentCategory>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(DocumentCategory category, CancellationToken ct = default);
    void Update(DocumentCategory category);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
