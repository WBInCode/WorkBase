using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Application.Contracts;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Employee?> GetByEmployeeNumberAsync(Guid tenantId, string employeeNumber, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsInTenantAsync(Guid tenantId, string email, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    void Update(Employee employee);

    /// <summary>Explicit save for code running OUTSIDE the MediatR pipeline's UnitOfWorkBehavior
    /// (e.g. domain-event handlers dispatched after SaveChanges) — their changes would
    /// otherwise never be persisted.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<(List<Employee> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId,
        string? search,
        Guid? organizationUnitId,
        EmployeeStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, (Guid UnitId, string UnitName)>> GetPrimaryAssignmentsAsync(
        IEnumerable<Guid> employeeIds,
        CancellationToken cancellationToken = default);
}
