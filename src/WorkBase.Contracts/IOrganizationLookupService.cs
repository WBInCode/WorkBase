namespace WorkBase.Contracts;

public interface IOrganizationLookupService
{
    Task<List<Guid>> GetEmployeeIdsByOrgUnitAsync(Guid tenantId, Guid orgUnitId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetAncestorOrgUnitIdsAsync(Guid orgUnitId, CancellationToken cancellationToken = default);

    /// <summary>Konto Keycloak przypisane do pracownika, albo null gdy pracownik go nie ma.</summary>
    Task<Guid?> GetUserIdByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
