using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed class AdvanceWorkflowHandler(IWorkflowEngine workflowEngine)
    : ICommandHandler<AdvanceWorkflowCommand, string>
{
    public async Task<Result<string>> Handle(AdvanceWorkflowCommand request, CancellationToken cancellationToken)
    {
        return await workflowEngine.AdvanceStepAsync(
            request.InstanceId,
            request.Outcome,
            request.CompletedBy,
            request.Comment,
            cancellationToken);
    }
}
