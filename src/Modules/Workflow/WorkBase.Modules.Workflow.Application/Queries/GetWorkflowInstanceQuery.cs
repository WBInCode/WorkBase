using WorkBase.Modules.Workflow.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Workflow.Application.Queries;

public sealed record GetWorkflowInstanceQuery(Guid InstanceId) : IQuery<WorkflowInstanceDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
