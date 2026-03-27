using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed record AdvanceWorkflowCommand(
    Guid InstanceId,
    string Outcome,
    string? CompletedBy = null,
    string? Comment = null) : ICommand<string>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
