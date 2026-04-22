using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record UpdateBreakPolicyCommand(
    Guid PolicyId,
    string Name,
    int? MaxPerDay,
    int? MaxMinutesPerBreak,
    int? MaxMinutesPerDay,
    bool IsActive) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}
