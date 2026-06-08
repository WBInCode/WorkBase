using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record CreateOrgUnitScheduleCommand(
    Guid OrgUnitId,
    string Name,
    string WeekPattern,
    DateOnly EffectiveFrom) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
