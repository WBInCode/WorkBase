using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Queries;

public sealed class GetWorkflowStepsHandler(
    IWorkflowStepRepository stepRepository)
    : IQueryHandler<GetWorkflowStepsQuery, List<WorkflowStepDto>>
{
    public async Task<Result<List<WorkflowStepDto>>> Handle(
        GetWorkflowStepsQuery request, CancellationToken cancellationToken)
    {
        var steps = await stepRepository.GetStepsByInstanceAsync(request.InstanceId, cancellationToken);

        var dtos = steps.Select(s => new WorkflowStepDto(
            s.Id,
            s.StepName,
            s.Status,
            s.EnteredAt,
            s.CompletedAt,
            s.CompletedBy,
            s.Outcome,
            s.Comment)).ToList();

        return dtos;
    }
}
