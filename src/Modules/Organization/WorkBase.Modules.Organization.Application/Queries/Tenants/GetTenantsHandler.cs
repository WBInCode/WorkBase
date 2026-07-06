using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Queries.Tenants;

public sealed class GetTenantsHandler(ITenantRepository tenantRepository)
    : IQueryHandler<GetTenantsQuery, List<TenantSummaryDto>>
{
    public async Task<Result<List<TenantSummaryDto>>> Handle(
        GetTenantsQuery request,
        CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.GetAllAsync(cancellationToken);

        return tenants
            .Select(t => new TenantSummaryDto(
                t.Id,
                t.Name,
                t.Slug,
                t.KeycloakRealmName,
                t.LicensePlanId,
                t.Status.ToString(),
                t.IsActive,
                t.TrialExpiresAt))
            .ToList();
    }
}
