namespace WorkBase.Contracts;

public interface IOrganizationLookupService
{
    Task<List<Guid>> GetEmployeeIdsByOrgUnitAsync(Guid tenantId, Guid orgUnitId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetAncestorOrgUnitIdsAsync(Guid orgUnitId, CancellationToken cancellationToken = default);
}
