using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Repositories;

public sealed class ApprovalDecisionRepository(WorkBaseDbContext dbContext) : IApprovalDecisionRepository
{
    public async Task<ApprovalDecision?> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<ApprovalDecision>()
            .FirstOrDefaultAsync(d => d.RequestId == requestId, cancellationToken);
    }

    public async Task AddAsync(ApprovalDecision decision, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<ApprovalDecision>().AddAsync(decision, cancellationToken);
    }
}
