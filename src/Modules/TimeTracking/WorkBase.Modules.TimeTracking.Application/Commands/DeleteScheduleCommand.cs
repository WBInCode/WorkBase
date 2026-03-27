using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record DeleteScheduleCommand(
    Guid ScheduleId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}
