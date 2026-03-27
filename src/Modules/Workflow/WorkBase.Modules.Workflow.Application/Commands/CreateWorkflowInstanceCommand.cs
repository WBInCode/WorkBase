using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed record CreateWorkflowInstanceCommand(
    Guid DefinitionId,
    string EntityType,
    Guid EntityId,
    Guid InitiatedBy) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
