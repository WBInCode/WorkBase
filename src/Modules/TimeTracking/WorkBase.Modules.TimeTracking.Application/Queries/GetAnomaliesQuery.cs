using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed record GetAnomaliesQuery(
    DateOnly From,
    DateOnly To,
    string? Status = null) : IQuery<IReadOnlyList<TimeAnomalyDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
