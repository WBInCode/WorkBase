using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record DeleteBreakPolicyCommand(Guid PolicyId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}
