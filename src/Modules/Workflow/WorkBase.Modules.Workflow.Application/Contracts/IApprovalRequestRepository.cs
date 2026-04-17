using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Application.Contracts;

public interface IApprovalRequestRepository
{
    Task<ApprovalRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApprovalRequest?> GetPendingByStepAsync(Guid stepId, CancellationToken cancellationToken = default);
    Task<List<ApprovalRequest>> GetPendingByApproverAsync(Guid tenantId, Guid approverId, CancellationToken cancellationToken = default);
    Task<List<ApprovalRequest>> GetByInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task AddAsync(ApprovalRequest request, CancellationToken cancellationToken = default);
    void Update(ApprovalRequest request);
}
