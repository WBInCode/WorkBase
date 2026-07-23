namespace WorkBase.Contracts;

public interface IEmployeeAccessStatusService
{
    Task<EmployeeAccessStatus?> GetAsync(
        Guid tenantId,
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<bool> RetryAsync(
        Guid tenantId,
        Guid employeeId,
        CancellationToken cancellationToken = default);
}

public sealed record EmployeeAccessStatus(
    bool ManagedByHub,
    string? Status,
    int Attempts,
    DateTime? UpdatedAt);