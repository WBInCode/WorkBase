using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Application.Contracts;

public interface IEmployeeAssignmentRepository
{
    Task<EmployeeAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmployeeAssignment?> GetPrimaryByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<List<EmployeeAssignment>> GetByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<List<EmployeeAssignment>> GetByOrgUnitAsync(Guid orgUnitId, CancellationToken cancellationToken = default);
    Task AddAsync(EmployeeAssignment assignment, CancellationToken cancellationToken = default);
    void Update(EmployeeAssignment assignment);
    void RemoveRange(IEnumerable<EmployeeAssignment> assignments);
}
