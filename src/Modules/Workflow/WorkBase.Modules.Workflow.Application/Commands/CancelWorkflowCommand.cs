using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed record CancelWorkflowCommand(
    Guid InstanceId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}
