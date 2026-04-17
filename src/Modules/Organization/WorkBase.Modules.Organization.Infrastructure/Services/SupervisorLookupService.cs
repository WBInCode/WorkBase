using Microsoft.EntityFrameworkCore;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Services;

public sealed class SupervisorLookupService(WorkBaseDbContext dbContext) : ISupervisorLookupService
{
    public async Task<Guid?> GetSupervisorEmployeeIdAsync(Guid subordinateEmployeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<SupervisorRelation>()
            .Where(r => r.SubordinateEmployeeId == subordinateEmployeeId && r.EndDate == null)
            .Select(r => (Guid?)r.SupervisorEmployeeId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> GetEmployeeIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Employee>()
            .Where(e => e.UserId == userId)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
