namespace WorkBase.Contracts;

/// <summary>
/// Onboards a new company (tenant): creates the Tenant record, seeds its own baseline
/// RBAC (roles/permissions/data scopes), and provisions the company's first admin account
/// (Keycloak user with a temporary password + linked application user with the tenant's
/// Admin role) — see docs/05-module-licensing-architecture.md. Implemented in
/// WorkBase.Infrastructure, which already has access to both the Organization module's
/// Tenant entity and the Identity module's Role/Permission/User entities (this cross-module
/// orchestration cannot live in either module directly without violating module boundaries).
/// </summary>
public interface ITenantProvisioningService
{
    Task<TenantProvisioningResult> CreateTenantAsync(
        string name,
        string slug,
        string adminEmail,
        string adminFirstName,
        string adminLastName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures that one HUB organization is represented by exactly one WorkBase tenant.
    /// Unlike operator onboarding, HUB owns user authentication, so this operation creates
    /// only the tenant and its baseline RBAC. It is safe to call repeatedly.
    /// </summary>
    Task<HubTenantProvisioningResult> EnsureHubTenantAsync(
        HubTenantRegistration registration,
        CancellationToken cancellationToken = default);
}

public sealed record HubTenantRegistration(
    string OrganizationId,
    string ProductInstanceId,
    string OrganizationName,
    string OrganizationSlug,
    Guid? ExistingTenantId = null);

public sealed record HubTenantProvisioningResult(Guid TenantId, bool Created);

/// <summary>
/// <paramref name="AdminTemporaryPassword"/> is only non-null when the Keycloak account was
/// actually created by this call — it is shown to the operator exactly once and never stored.
/// Null means Keycloak provisioning was skipped/failed (e.g. admin credentials not configured)
/// and the account must be created manually in Keycloak.
/// <paramref name="KeycloakRealmName"/> is the tenant's dedicated realm (multi-realm mode) or
/// null when the tenant lives on the shared realm.
/// </summary>
public sealed record TenantProvisioningResult(
    Guid TenantId,
    string AdminEmail,
    string? AdminTemporaryPassword,
    string? KeycloakRealmName = null);
