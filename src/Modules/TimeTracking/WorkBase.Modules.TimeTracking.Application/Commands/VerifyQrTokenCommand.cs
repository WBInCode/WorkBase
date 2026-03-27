using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record VerifyQrTokenCommand(
    string Token,
    Guid EmployeeId) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
