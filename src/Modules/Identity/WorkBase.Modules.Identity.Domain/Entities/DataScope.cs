using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Identity.Domain.Entities;

public sealed class DataScope : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;
    public string Module { get; private set; } = null!;
    public DataScopeLevel ScopeLevel { get; private set; }
    public string? CustomFilter { get; private set; }

    private DataScope() { }

    public static DataScope Create(
        Guid tenantId,
        Guid roleId,
        string module,
        DataScopeLevel scopeLevel,
        string? customFilter = null)
    {
        return new DataScope
        {
            TenantId = tenantId,
            RoleId = roleId,
            Module = module,
            ScopeLevel = scopeLevel,
            CustomFilter = customFilter
        };
    }

    public void Update(DataScopeLevel scopeLevel, string? customFilter)
    {
        ScopeLevel = scopeLevel;
        CustomFilter = customFilter;
    }
}

public enum DataScopeLevel
{
    Own,
    Team,
    Department,
    Branch,
    Organization
}
