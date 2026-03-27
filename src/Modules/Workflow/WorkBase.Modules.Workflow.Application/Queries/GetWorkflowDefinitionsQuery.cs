using WorkBase.Modules.Workflow.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Workflow.Application.Queries;

public sealed record GetWorkflowDefinitionsQuery : IQuery<List<WorkflowDefinitionDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
