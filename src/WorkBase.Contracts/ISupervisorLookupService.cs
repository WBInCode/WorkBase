namespace WorkBase.Contracts;

/// <summary>
/// Cross-module contract for resolving supervisor of an employee.
/// Implemented by Organization module, consumed by Workflow module.
/// </summary>
public interface ISupervisorLookupService
{
    /// <summary>
    /// Returns the supervisor employee ID for the given subordinate employee,
    /// or null if no active supervisor relation exists.
    /// </summary>
    Task<Guid?> GetSupervisorEmployeeIdAsync(Guid subordinateEmployeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the employee ID for the given Keycloak/Identity user ID,
    /// or null if no employee is linked to that user.
    /// </summary>
    Task<Guid?> GetEmployeeIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
