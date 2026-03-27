using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed record UpdateWorkflowDefinitionCommand(
    Guid DefinitionId,
    string Name,
    string DefinitionJson,
    string? Description = null) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}
