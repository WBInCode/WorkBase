using WorkBase.Modules.Workflow.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Workflow.Application.Queries;

public sealed record GetWorkflowStepsQuery(Guid InstanceId) : IQuery<List<WorkflowStepDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
