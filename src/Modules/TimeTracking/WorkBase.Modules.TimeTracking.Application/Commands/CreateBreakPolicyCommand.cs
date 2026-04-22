using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record CreateBreakPolicyCommand(
    string Name,
    BreakType BreakType,
    int? MaxPerDay,
    int? MaxMinutesPerBreak,
    int? MaxMinutesPerDay,
    bool IsActive = true) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
