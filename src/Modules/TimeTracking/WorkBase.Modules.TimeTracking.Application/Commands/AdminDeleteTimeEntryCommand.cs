using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record AdminDeleteTimeEntryCommand(Guid EntryId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}
