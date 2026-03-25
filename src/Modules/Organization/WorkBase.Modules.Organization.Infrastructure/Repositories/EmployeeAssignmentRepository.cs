using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Repositories;

public sealed class EmployeeAssignmentRepository(WorkBaseDbContext dbContext) : IEmployeeAssignmentRepository
{
    public async Task<EmployeeAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<EmployeeAssignment>()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<EmployeeAssignment?> GetPrimaryByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<EmployeeAssignment>()
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.IsPrimary && a.EndDate == null, cancellationToken);
    }

    public async Task<List<EmployeeAssignment>> GetByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<EmployeeAssignment>()
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.IsPrimary)
            .ThenByDescending(a => a.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(EmployeeAssignment assignment, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<EmployeeAssignment>().AddAsync(assignment, cancellationToken);
    }

    public void Update(EmployeeAssignment assignment)
    {
        dbContext.Set<EmployeeAssignment>().Update(assignment);
    }
}
