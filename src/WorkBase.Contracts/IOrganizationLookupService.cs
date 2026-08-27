namespace WorkBase.Contracts;

public interface IOrganizationLookupService
{
    Task<List<Guid>> GetEmployeeIdsByOrgUnitAsync(Guid tenantId, Guid orgUnitId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetAncestorOrgUnitIdsAsync(Guid orgUnitId, CancellationToken cancellationToken = default);

    /// <summary>Konto Keycloak przypisane do pracownika, albo null gdy pracownik go nie ma.</summary>
    Task<Guid?> GetUserIdByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>Imię i nazwisko pracownika, albo null gdy nie ma takiego pracownika.</summary>
    Task<string?> GetEmployeeFullNameAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adres e-mail właściciela konta, albo null gdy konto nie ma kartoteki pracownika
    /// lub kartoteka nie ma adresu.
    /// </summary>
    Task<string?> GetEmailByUserIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
}
