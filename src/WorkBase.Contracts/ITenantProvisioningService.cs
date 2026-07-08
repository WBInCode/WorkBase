namespace WorkBase.Contracts;

/// <summary>
/// Onboards a new company (tenant): creates the Tenant record and seeds its own baseline
/// RBAC (roles/permissions/data scopes) so its first users actually get working access —
/// see docs/05-module-licensing-architecture.md. Implemented in WorkBase.Infrastructure,
/// which already has access to both the Organization module's Tenant entity and the
/// Identity module's Role/Permission entities (this cross-module orchestration cannot live
/// in either module directly without violating module boundaries).
/// </summary>
public interface ITenantProvisioningService
{
    Task<Guid> CreateTenantAsync(string name, string slug, CancellationToken cancellationToken = default);
}
