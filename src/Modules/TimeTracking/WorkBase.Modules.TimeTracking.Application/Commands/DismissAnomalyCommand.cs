using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record DismissAnomalyCommand(
    Guid AnomalyId,
    string ReviewedBy) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}
