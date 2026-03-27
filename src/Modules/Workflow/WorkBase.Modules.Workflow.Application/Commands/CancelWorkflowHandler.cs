using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed class CancelWorkflowHandler(IWorkflowEngine workflowEngine)
    : ICommandHandler<CancelWorkflowCommand>
{
    public async Task<Result> Handle(CancelWorkflowCommand request, CancellationToken cancellationToken)
    {
        return await workflowEngine.CancelInstanceAsync(request.InstanceId, cancellationToken);
    }
}
