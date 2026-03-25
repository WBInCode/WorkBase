using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Application.Contracts;

public interface IOrganizationUnitTypeRepository
{
    Task<OrganizationUnitType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsInTenantAsync(Guid tenantId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<List<OrganizationUnitType>> GetAllByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(OrganizationUnitType unitType, CancellationToken cancellationToken = default);
    void Update(OrganizationUnitType unitType);
}
