using WorkBase.Modules.Identity.Domain.Entities;

namespace WorkBase.Modules.Identity.Application.Contracts;

public interface IDataScopeManagementService
{
    Task<List<DataScope>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<Guid> CreateAsync(Guid tenantId, Guid roleId, string module, DataScopeLevel scopeLevel, string? customFilter, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, DataScopeLevel scopeLevel, string? customFilter, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
