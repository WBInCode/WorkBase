using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record ClockOutCommand(
    Guid EmployeeId,
    string? Note = null,
    string? IpAddress = null,
    string? Location = null) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
