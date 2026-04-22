using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record StartBreakCommand(
    Guid EmployeeId,
    BreakType BreakType,
    string? Note = null) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
