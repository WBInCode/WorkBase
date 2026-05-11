using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record UpdateOrgUnitScheduleCommand(
    Guid Id,
    string Name,
    string WeekPattern,
    DateOnly EffectiveFrom) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}
