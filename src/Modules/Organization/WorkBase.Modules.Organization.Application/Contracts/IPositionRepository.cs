using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Application.Contracts;

public interface IPositionRepository
{
    Task<Position?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsInTenantAsync(Guid tenantId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<List<Position>> GetAllByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(Position position, CancellationToken cancellationToken = default);
    void Update(Position position);
}
