using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Infrastructure.Repositories;

public sealed class LeavePolicyRepository(WorkBaseDbContext dbContext) : ILeavePolicyRepository
{
    public async Task<LeavePolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Set<LeavePolicy>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<LeavePolicy?> GetActiveByTypeAsync(Guid tenantId, Guid leaveTypeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<LeavePolicy>()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.LeaveTypeId == leaveTypeId && p.IsActive, cancellationToken);
    }

    public async Task<List<LeavePolicy>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await dbContext.Set<LeavePolicy>().Where(p => p.TenantId == tenantId).OrderBy(p => p.Name).ToListAsync(cancellationToken);

    public async Task AddAsync(LeavePolicy policy, CancellationToken cancellationToken = default)
        => await dbContext.Set<LeavePolicy>().AddAsync(policy, cancellationToken);

    public void Update(LeavePolicy policy) => dbContext.Set<LeavePolicy>().Update(policy);
}
