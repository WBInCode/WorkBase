using WorkBase.Modules.Cases.Domain.Entities;

namespace WorkBase.Modules.Cases.Application.Contracts;

public interface ICaseItemRepository
{
    Task<CaseItem?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<CaseItem>> GetByTenantAsync(Guid tenantId, CancellationToken ct);
    Task<List<CaseItem>> GetByAssigneeAsync(Guid tenantId, Guid assigneeId, CancellationToken ct);
    Task<List<CaseItem>> GetByContactAsync(Guid tenantId, Guid contactId, CancellationToken ct);
    Task<List<CaseItem>> GetSlaBreachedAsync(Guid tenantId, CancellationToken ct);
    Task<int> GetNextNumberAsync(Guid tenantId, CancellationToken ct);
    Task AddAsync(CaseItem item, CancellationToken ct);
    void Update(CaseItem item);
    void Remove(CaseItem item);
}

public interface ICaseStatusRepository
{
    Task<CaseStatus?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<CaseStatus?> GetDefaultAsync(Guid tenantId, CancellationToken ct);
    Task<List<CaseStatus>> GetByTenantAsync(Guid tenantId, CancellationToken ct);
    Task AddAsync(CaseStatus status, CancellationToken ct);
    void Update(CaseStatus status);
    void Remove(CaseStatus status);
}

public interface ICaseCategoryRepository
{
    Task<CaseCategory?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<CaseCategory>> GetByTenantAsync(Guid tenantId, CancellationToken ct);
    Task AddAsync(CaseCategory category, CancellationToken ct);
    void Update(CaseCategory category);
    void Remove(CaseCategory category);
}

public interface ICaseCommentRepository
{
    Task<List<CaseComment>> GetByCaseAsync(Guid caseId, CancellationToken ct);
    Task AddAsync(CaseComment comment, CancellationToken ct);
    void Update(CaseComment comment);
    void Remove(CaseComment comment);
}
