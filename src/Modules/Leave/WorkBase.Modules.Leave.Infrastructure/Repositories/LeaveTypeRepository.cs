using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Infrastructure.Repositories;

public sealed class LeaveTypeRepository(WorkBaseDbContext dbContext) : ILeaveTypeRepository
{
    public async Task<LeaveType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<LeaveType>()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<LeaveType>> GetActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<LeaveType>()
            .Where(t => t.TenantId == tenantId && t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);
    }
}
