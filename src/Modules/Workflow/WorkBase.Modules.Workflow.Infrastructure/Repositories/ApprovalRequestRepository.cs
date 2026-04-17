using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Repositories;

public sealed class ApprovalRequestRepository(WorkBaseDbContext dbContext) : IApprovalRequestRepository
{
    public async Task<ApprovalRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<ApprovalRequest>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<ApprovalRequest?> GetPendingByStepAsync(Guid stepId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<ApprovalRequest>()
            .FirstOrDefaultAsync(r => r.StepId == stepId && r.Status == "Pending", cancellationToken);
    }

    public async Task<List<ApprovalRequest>> GetPendingByApproverAsync(Guid tenantId, Guid approverId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<ApprovalRequest>()
            .Where(r => r.TenantId == tenantId && r.ApproverId == approverId && r.Status == "Pending")
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ApprovalRequest>> GetByInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<ApprovalRequest>()
            .Where(r => r.InstanceId == instanceId)
            .OrderBy(r => r.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<ApprovalRequest>().AddAsync(request, cancellationToken);
    }

    public void Update(ApprovalRequest request)
    {
        dbContext.Set<ApprovalRequest>().Update(request);
    }
}
