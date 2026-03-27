using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed class GetAnomaliesHandler(ITimeAnomalyRepository anomalyRepository)
    : IQueryHandler<GetAnomaliesQuery, IReadOnlyList<TimeAnomalyDto>>
{
    public async Task<Result<IReadOnlyList<TimeAnomalyDto>>> Handle(
        GetAnomaliesQuery request,
        CancellationToken cancellationToken)
    {
        AnomalyStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<AnomalyStatus>(request.Status, ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }

        var anomalies = await anomalyRepository.GetByDateRangeAsync(
            request.TenantId, request.From, request.To, statusFilter, cancellationToken);

        var dtos = anomalies.Select(a => new TimeAnomalyDto(
            a.Id,
            a.EmployeeId,
            a.Date,
            a.Type.ToString(),
            a.Status.ToString(),
            a.Description,
            a.Details,
            a.ReviewedBy,
            a.ReviewedAt,
            a.CreatedAt)).ToList();

        return dtos;
    }
}
