using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Application.Contracts;

public interface ITenantRepository
{
    /// <summary>Lists every tenant in the system, regardless of caller's own TenantId. Used
    /// exclusively by platform-operator endpoints (docs/05-module-licensing-architecture.md
    /// step 5) — callers must be gated separately (see PlatformOperatorEndpointFilter), this
    /// repository itself performs no tenant-scoping/authorization.</summary>
    Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
