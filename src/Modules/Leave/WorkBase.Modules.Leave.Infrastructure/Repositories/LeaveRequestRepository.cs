using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Infrastructure.Repositories;

public sealed class LeaveRequestRepository(WorkBaseDbContext dbContext) : ILeaveRequestRepository
{
    public async Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<LeaveRequest>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<List<LeaveRequest>> GetByEmployeeAsync(Guid tenantId, Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<LeaveRequest>()
            .Where(r => r.TenantId == tenantId && r.EmployeeId == employeeId)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOverlappingRequestAsync(
        Guid tenantId, Guid employeeId, DateTime startDate, DateTime endDate,
        Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<LeaveRequest>()
            .Where(r => r.TenantId == tenantId
                && r.EmployeeId == employeeId
                && r.Status != LeaveRequestStatus.Cancelled
                && r.Status != LeaveRequestStatus.Rejected
                && r.StartDate <= endDate
                && r.EndDate >= startDate);

        if (excludeId.HasValue)
            query = query.Where(r => r.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(LeaveRequest request, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<LeaveRequest>().AddAsync(request, cancellationToken);
    }

    public void Update(LeaveRequest request)
    {
        dbContext.Set<LeaveRequest>().Update(request);
    }
}
