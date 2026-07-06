using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Queries.Tenants;

/// <summary>
/// Cross-tenant query: intentionally does NOT implement ITenantRequest, since it lists ALL
/// tenants rather than being scoped to the caller's own. Authorization is enforced at the
/// endpoint via RequirePlatformOperator(), not via TenantBehavior.
/// </summary>
public sealed record GetTenantsQuery : IQuery<List<TenantSummaryDto>>;
