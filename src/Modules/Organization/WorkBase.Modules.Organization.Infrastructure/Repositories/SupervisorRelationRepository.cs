using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Repositories;

public sealed class SupervisorRelationRepository(WorkBaseDbContext dbContext) : ISupervisorRelationRepository
{
    public async Task<SupervisorRelation?> GetActiveBySubordinateAsync(Guid subordinateEmployeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<SupervisorRelation>()
            .FirstOrDefaultAsync(r => r.SubordinateEmployeeId == subordinateEmployeeId && r.EndDate == null, cancellationToken);
    }

    public async Task AddAsync(SupervisorRelation relation, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<SupervisorRelation>().AddAsync(relation, cancellationToken);
    }

    public void Update(SupervisorRelation relation)
    {
        dbContext.Set<SupervisorRelation>().Update(relation);
    }
}
