using WorkBase.Modules.Dashboard.Application.Contracts;
using WorkBase.Modules.Dashboard.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Dashboard.Application.Queries;

public sealed record GetDashboardSummaryQuery(IReadOnlyList<Guid>? VisibleUnitIds = null) : IQuery<DashboardSummaryDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetDashboardSummaryHandler(IDashboardQueryService queryService)
    : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var summary = await queryService.GetSummaryAsync(
            request.TenantId, request.VisibleUnitIds, cancellationToken);
        return summary;
    }
}
