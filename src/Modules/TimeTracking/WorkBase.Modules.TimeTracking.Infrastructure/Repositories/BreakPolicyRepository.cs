using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Repositories;

public sealed class BreakPolicyRepository(WorkBaseDbContext dbContext) : IBreakPolicyRepository
{
    public async Task<List<BreakPolicy>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await dbContext.Set<BreakPolicy>()
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.BreakType)
            .ToListAsync(ct);
    }

    public async Task<BreakPolicy?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        return await dbContext.Set<BreakPolicy>()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == id, ct);
    }

    public async Task<BreakPolicy?> GetByTypeAsync(Guid tenantId, BreakType breakType, CancellationToken ct = default)
    {
        return await dbContext.Set<BreakPolicy>()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.BreakType == breakType && p.IsActive, ct);
    }

    public async Task AddAsync(BreakPolicy policy, CancellationToken ct = default)
    {
        await dbContext.Set<BreakPolicy>().AddAsync(policy, ct);
    }

    public void Update(BreakPolicy policy)
    {
        dbContext.Set<BreakPolicy>().Update(policy);
    }

    public void Remove(BreakPolicy policy)
    {
        dbContext.Set<BreakPolicy>().Remove(policy);
    }
}
