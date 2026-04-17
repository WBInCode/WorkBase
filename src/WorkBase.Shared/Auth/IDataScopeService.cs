namespace WorkBase.Shared.Auth;

public interface IDataScopeService
{
    Task<DataScopeResult> GetEffectiveScopeAsync(Guid userId, Guid tenantId, string module, CancellationToken ct = default);
}

public sealed record DataScopeResult(DataScopeLevelValue Level)
{
    public bool IsOrganization => Level == DataScopeLevelValue.Organization;
    public bool IsOwn => Level == DataScopeLevelValue.Own;
}

public enum DataScopeLevelValue
{
    Own = 0,
    Team = 1,
    Department = 2,
    Branch = 3,
    Organization = 4
}
