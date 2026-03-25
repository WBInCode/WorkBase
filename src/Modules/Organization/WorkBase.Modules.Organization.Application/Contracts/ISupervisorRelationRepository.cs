using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Application.Contracts;

public interface ISupervisorRelationRepository
{
    Task<SupervisorRelation?> GetActiveBySubordinateAsync(Guid subordinateEmployeeId, CancellationToken cancellationToken = default);
    Task AddAsync(SupervisorRelation relation, CancellationToken cancellationToken = default);
    void Update(SupervisorRelation relation);
}
