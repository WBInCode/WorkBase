using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Infrastructure.Repositories;

public sealed class LeaveBalanceRepository(WorkBaseDbContext dbContext) : ILeaveBalanceRepository
{
    public async Task<LeaveBalance?> GetAsync(
        Guid tenantId, Guid employeeId, Guid leaveTypeId, int year,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<LeaveBalance>()
            .FirstOrDefaultAsync(b =>
                b.TenantId == tenantId
                && b.EmployeeId == employeeId
                && b.LeaveTypeId == leaveTypeId
                && b.Year == year, cancellationToken);
    }

    public async Task<List<LeaveBalance>> GetByEmployeeAsync(
        Guid tenantId, Guid employeeId, int year,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<LeaveBalance>()
            .Where(b => b.TenantId == tenantId && b.EmployeeId == employeeId && b.Year == year)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(LeaveBalance balance, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<LeaveBalance>().AddAsync(balance, cancellationToken);
    }

    public void Update(LeaveBalance balance)
    {
        dbContext.Set<LeaveBalance>().Update(balance);
    }
}
