using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Repositories;

public sealed class EscalationRuleRepository(WorkBaseDbContext db) : IEscalationRuleRepository
{
    public async Task<EscalationRule?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<EscalationRule>().FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<List<EscalationRule>> GetByDefinitionAsync(Guid tenantId, Guid definitionId, CancellationToken ct = default)
        => await db.Set<EscalationRule>()
            .Where(r => r.TenantId == tenantId && r.DefinitionId == definitionId)
            .OrderBy(r => r.StepName)
            .ToListAsync(ct);

    public async Task<List<EscalationRule>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await db.Set<EscalationRule>()
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.DefinitionId).ThenBy(r => r.StepName)
            .ToListAsync(ct);

    public async Task AddAsync(EscalationRule rule, CancellationToken ct = default)
        => await db.Set<EscalationRule>().AddAsync(rule, ct);

    public void Remove(EscalationRule rule)
        => db.Set<EscalationRule>().Remove(rule);
}
