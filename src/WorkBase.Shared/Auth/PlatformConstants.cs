namespace WorkBase.Shared.Auth;

/// <summary>
/// Constants for the "platform operator" concept (docs/05-module-licensing-architecture.md §Step 5):
/// until true multi-realm, per-tenant platform-admin accounts exist (step 6), our own operator
/// company acts as a single, dedicated tenant whose Super Admins may manage OTHER tenants'
/// module licensing (list companies, apply plans, toggle flags).
/// </summary>
public static class PlatformConstants
{
    /// <summary>
    /// The tenant id of our own operator company (seeded by TenantSeeder as "WorkBase Development").
    /// Only users authenticated in THIS tenant, holding <see cref="ManageTenantsPermission"/>,
    /// are allowed to act on other tenants' licensing via the platform-operator endpoints.
    /// </summary>
    public static readonly Guid OperatorTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>Permission required (in addition to being in the operator tenant) to manage other tenants.</summary>
    public const string ManageTenantsPermission = "platform.manage-tenants";
}
