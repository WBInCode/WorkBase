using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Infrastructure.Repositories;

public sealed class LeavePolicyRepository(WorkBaseDbContext dbContext) : ILeavePolicyRepository
{
    public async Task<LeavePolicy?> GetActiveByTypeAsync(Guid tenantId, Guid leaveTypeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<LeavePolicy>()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.LeaveTypeId == leaveTypeId && p.IsActive, cancellationToken);
    }
}
