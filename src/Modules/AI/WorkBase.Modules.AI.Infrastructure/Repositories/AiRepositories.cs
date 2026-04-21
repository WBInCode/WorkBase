using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.AI.Application.Contracts;
using WorkBase.Modules.AI.Domain.Entities;

namespace WorkBase.Modules.AI.Infrastructure.Repositories;

public sealed class AiTaskLogRepository(WorkBaseDbContext db) : IAiTaskLogRepository
{
    public async Task AddAsync(AiTaskLog log, CancellationToken ct) => await db.Set<AiTaskLog>().AddAsync(log, ct);
    public async Task<List<AiTaskLog>> GetRecentAsync(Guid tenantId, int count, CancellationToken ct) =>
        await db.Set<AiTaskLog>().Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.CreatedAt).Take(count).ToListAsync(ct);
}
