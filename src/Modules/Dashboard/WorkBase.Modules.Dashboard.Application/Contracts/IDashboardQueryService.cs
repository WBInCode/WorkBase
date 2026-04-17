using WorkBase.Modules.Dashboard.Application.Dtos;

namespace WorkBase.Modules.Dashboard.Application.Contracts;

public interface IDashboardQueryService
{
    Task<DashboardSummaryDto> GetSummaryAsync(Guid tenantId, IReadOnlyList<Guid>? visibleUnitIds, CancellationToken cancellationToken = default);
}
