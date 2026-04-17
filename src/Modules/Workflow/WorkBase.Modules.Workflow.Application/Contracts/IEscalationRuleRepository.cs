using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Application.Contracts;

public interface IEscalationRuleRepository
{
    Task<EscalationRule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<EscalationRule>> GetByDefinitionAsync(Guid tenantId, Guid definitionId, CancellationToken ct = default);
    Task<List<EscalationRule>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(EscalationRule rule, CancellationToken ct = default);
    void Remove(EscalationRule rule);
}
