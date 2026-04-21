using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Cases.Application.Contracts;
using WorkBase.Modules.Cases.Domain.Entities;

namespace WorkBase.Modules.Cases.Infrastructure.Repositories;

public sealed class CaseItemRepository(WorkBaseDbContext db) : ICaseItemRepository
{
    public async Task<CaseItem?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.Set<CaseItem>().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<List<CaseItem>> GetByTenantAsync(Guid tenantId, CancellationToken ct)
        => await db.Set<CaseItem>().Where(e => e.TenantId == tenantId).OrderByDescending(e => e.CreatedAt).ToListAsync(ct);

    public async Task<List<CaseItem>> GetByAssigneeAsync(Guid tenantId, Guid assigneeId, CancellationToken ct)
        => await db.Set<CaseItem>().Where(e => e.TenantId == tenantId && e.AssigneeId == assigneeId).OrderByDescending(e => e.CreatedAt).ToListAsync(ct);

    public async Task<List<CaseItem>> GetByContactAsync(Guid tenantId, Guid contactId, CancellationToken ct)
        => await db.Set<CaseItem>().Where(e => e.TenantId == tenantId && e.ContactId == contactId).OrderByDescending(e => e.CreatedAt).ToListAsync(ct);

    public async Task<List<CaseItem>> GetSlaBreachedAsync(Guid tenantId, CancellationToken ct)
        => await db.Set<CaseItem>().Where(e => e.TenantId == tenantId && e.DueDate != null && e.ResolvedAt == null && e.DueDate < DateTime.UtcNow).ToListAsync(ct);

    public async Task<int> GetNextNumberAsync(Guid tenantId, CancellationToken ct)
        => await db.Set<CaseItem>().Where(e => e.TenantId == tenantId).CountAsync(ct) + 1;

    public async Task AddAsync(CaseItem item, CancellationToken ct) => await db.Set<CaseItem>().AddAsync(item, ct);
    public void Update(CaseItem item) => db.Set<CaseItem>().Update(item);
    public void Remove(CaseItem item) => db.Set<CaseItem>().Remove(item);
}

public sealed class CaseStatusRepository(WorkBaseDbContext db) : ICaseStatusRepository
{
    public async Task<CaseStatus?> GetByIdAsync(Guid id, CancellationToken ct) => await db.Set<CaseStatus>().FirstOrDefaultAsync(e => e.Id == id, ct);
    public async Task<CaseStatus?> GetDefaultAsync(Guid tenantId, CancellationToken ct) => await db.Set<CaseStatus>().FirstOrDefaultAsync(e => e.TenantId == tenantId && e.IsDefault, ct);
    public async Task<List<CaseStatus>> GetByTenantAsync(Guid tenantId, CancellationToken ct) => await db.Set<CaseStatus>().Where(e => e.TenantId == tenantId).OrderBy(e => e.SortOrder).ToListAsync(ct);
    public async Task AddAsync(CaseStatus status, CancellationToken ct) => await db.Set<CaseStatus>().AddAsync(status, ct);
    public void Update(CaseStatus status) => db.Set<CaseStatus>().Update(status);
    public void Remove(CaseStatus status) => db.Set<CaseStatus>().Remove(status);
}

public sealed class CaseCategoryRepository(WorkBaseDbContext db) : ICaseCategoryRepository
{
    public async Task<CaseCategory?> GetByIdAsync(Guid id, CancellationToken ct) => await db.Set<CaseCategory>().FirstOrDefaultAsync(e => e.Id == id, ct);
    public async Task<List<CaseCategory>> GetByTenantAsync(Guid tenantId, CancellationToken ct) => await db.Set<CaseCategory>().Where(e => e.TenantId == tenantId).OrderBy(e => e.Name).ToListAsync(ct);
    public async Task AddAsync(CaseCategory category, CancellationToken ct) => await db.Set<CaseCategory>().AddAsync(category, ct);
    public void Update(CaseCategory category) => db.Set<CaseCategory>().Update(category);
    public void Remove(CaseCategory category) => db.Set<CaseCategory>().Remove(category);
}

public sealed class CaseCommentRepository(WorkBaseDbContext db) : ICaseCommentRepository
{
    public async Task<List<CaseComment>> GetByCaseAsync(Guid caseId, CancellationToken ct) => await db.Set<CaseComment>().Where(e => e.CaseId == caseId).OrderBy(e => e.CreatedAt).ToListAsync(ct);
    public async Task AddAsync(CaseComment comment, CancellationToken ct) => await db.Set<CaseComment>().AddAsync(comment, ct);
    public void Update(CaseComment comment) => db.Set<CaseComment>().Update(comment);
    public void Remove(CaseComment comment) => db.Set<CaseComment>().Remove(comment);
}
