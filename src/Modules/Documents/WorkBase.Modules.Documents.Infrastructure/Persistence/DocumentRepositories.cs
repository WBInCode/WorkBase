using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Documents.Application.Contracts;
using WorkBase.Modules.Documents.Domain.Entities;

namespace WorkBase.Modules.Documents.Infrastructure.Persistence;

public sealed class DocumentRepository(WorkBaseDbContext db) : IDocumentRepository
{
    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<Document>().FindAsync([id], ct);

    public async Task<List<Document>> GetByTenantAsync(
        Guid tenantId, Guid? categoryId, string? entityType, Guid? entityId,
        bool includeDeleted, CancellationToken ct = default)
    {
        var query = db.Set<Document>().Where(d => d.TenantId == tenantId);

        if (!includeDeleted)
            query = query.Where(d => !d.IsDeleted);
        if (categoryId.HasValue)
            query = query.Where(d => d.CategoryId == categoryId.Value);
        if (entityType is not null)
            query = query.Where(d => d.EntityType == entityType);
        if (entityId.HasValue)
            query = query.Where(d => d.EntityId == entityId.Value);

        return await query.OrderByDescending(d => d.CreatedAt).ToListAsync(ct);
    }

    public async Task AddAsync(Document document, CancellationToken ct = default)
        => await db.Set<Document>().AddAsync(document, ct);

    public void Update(Document document) => db.Set<Document>().Update(document);

    public async Task SaveChangesAsync(CancellationToken ct = default) => await db.SaveChangesAsync(ct);
}

public sealed class DocumentCategoryRepository(WorkBaseDbContext db) : IDocumentCategoryRepository
{
    public async Task<DocumentCategory?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<DocumentCategory>().FindAsync([id], ct);

    public async Task<List<DocumentCategory>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await db.Set<DocumentCategory>().Where(c => c.TenantId == tenantId).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task AddAsync(DocumentCategory category, CancellationToken ct = default)
        => await db.Set<DocumentCategory>().AddAsync(category, ct);

    public void Update(DocumentCategory category) => db.Set<DocumentCategory>().Update(category);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Set<DocumentCategory>().FindAsync([id], ct);
        if (entity is null) return false;
        db.Set<DocumentCategory>().Remove(entity);
        return true;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default) => await db.SaveChangesAsync(ct);
}
