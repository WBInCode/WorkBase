using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record DeleteOrgUnitScheduleCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}
