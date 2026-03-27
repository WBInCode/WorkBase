using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed record CreateWorkflowDefinitionCommand(
    string Name,
    string DefinitionJson,
    string? Description = null) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
