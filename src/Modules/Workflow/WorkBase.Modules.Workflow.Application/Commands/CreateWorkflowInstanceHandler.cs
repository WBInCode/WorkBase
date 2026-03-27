using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed class CreateWorkflowInstanceHandler(IWorkflowEngine workflowEngine)
    : ICommandHandler<CreateWorkflowInstanceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateWorkflowInstanceCommand request, CancellationToken cancellationToken)
    {
        return await workflowEngine.CreateInstanceAsync(
            request.TenantId,
            request.DefinitionId,
            request.EntityType,
            request.EntityId,
            request.InitiatedBy,
            cancellationToken);
    }
}
