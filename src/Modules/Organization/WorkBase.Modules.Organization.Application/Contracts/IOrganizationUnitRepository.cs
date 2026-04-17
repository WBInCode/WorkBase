using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Application.Contracts;

public interface IOrganizationUnitRepository
{
    Task<OrganizationUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsInTenantAsync(Guid tenantId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsInTenantAsync(Guid tenantId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(OrganizationUnit unit, CancellationToken cancellationToken = default);
    void Update(OrganizationUnit unit);
    void Remove(OrganizationUnit unit);
    Task RebuildClosureAsync(Guid unitId, Guid? oldParentId, Guid? newParentId, CancellationToken cancellationToken = default);
    Task InsertClosureForNewUnitAsync(Guid unitId, Guid? parentId, CancellationToken cancellationToken = default);
    Task<List<OrganizationUnit>> GetAllByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<OrganizationUnitClosure>> GetClosureByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
