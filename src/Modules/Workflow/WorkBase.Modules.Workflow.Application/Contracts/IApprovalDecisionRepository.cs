using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Application.Contracts;

public interface IApprovalDecisionRepository
{
    Task<ApprovalDecision?> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task AddAsync(ApprovalDecision decision, CancellationToken cancellationToken = default);
}
