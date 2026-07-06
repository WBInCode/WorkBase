using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Identity.Application.Contracts;

public interface IFeatureFlagService
{
    Task<List<FeatureFlag>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task ToggleAsync(Guid tenantId, string module, string? userId, CancellationToken ct = default);

    /// <summary>
    /// Materializes a LicensePlan's IncludedModules into FeatureFlag rows for the given
    /// tenant: enables flags for modules in the plan, disables flags for modules not in
    /// the plan. Returns NotFound if no active plan with that id exists.
    /// </summary>
    Task<Result> ApplyPlanAsync(Guid tenantId, Guid planId, string? userId, CancellationToken ct = default);
}
