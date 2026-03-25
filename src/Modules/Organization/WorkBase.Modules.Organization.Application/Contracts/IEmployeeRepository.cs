using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Application.Contracts;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsInTenantAsync(Guid tenantId, string email, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    void Update(Employee employee);
    Task<(List<Employee> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId,
        string? search,
        Guid? organizationUnitId,
        EmployeeStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
