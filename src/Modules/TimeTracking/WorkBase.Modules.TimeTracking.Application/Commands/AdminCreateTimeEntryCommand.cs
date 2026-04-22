using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record AdminCreateTimeEntryCommand(
    Guid EmployeeId,
    DateTime EntryTime,
    string Type,
    string? BreakType = null,
    string? Note = null) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
