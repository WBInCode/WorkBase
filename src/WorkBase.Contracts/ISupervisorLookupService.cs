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

    /// <summary>Czy pracownik ma aktywnych podwładnych. Bycie przełożonym to relacja, nie rola.</summary>
    Task<bool> HasSubordinatesAsync(Guid supervisorEmployeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca osobę, która w danym dniu zastępuje wskazanego akceptanta, albo null gdy nikt go
    /// nie zastępuje. Zastępstwo dotyczy wyłącznie wskazania akceptanta — nie przenosi uprawnień
    /// ani zakresu danych.
    /// </summary>
    Task<Guid?> GetZastepceAsync(
        Guid zastepowanyEmployeeId, DateOnly dzien, CancellationToken cancellationToken = default);
}
