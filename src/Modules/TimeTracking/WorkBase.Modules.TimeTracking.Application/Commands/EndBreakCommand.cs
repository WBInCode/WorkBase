using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record EndBreakCommand(
    Guid EmployeeId,
    string? Note = null) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
