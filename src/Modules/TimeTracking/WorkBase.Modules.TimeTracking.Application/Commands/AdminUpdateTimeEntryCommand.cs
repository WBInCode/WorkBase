using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record AdminUpdateTimeEntryCommand(
    Guid EntryId,
    DateTime EntryTime,
    string Type,
    string? BreakType = null,
    string? Note = null) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}
