using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record ReviewAnomalyCommand(
    Guid AnomalyId,
    string ReviewedBy) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}
