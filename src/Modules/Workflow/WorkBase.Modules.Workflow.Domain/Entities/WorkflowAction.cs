using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Domain.Entities;

public sealed class WorkflowAction : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid StepId { get; private set; }
    public Guid InstanceId { get; private set; }
    public string ActionType { get; private set; } = null!;
    public string? PayloadJson { get; private set; }
    public string Status { get; private set; } = null!;
    public DateTime ExecutedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private WorkflowAction() { }

    public static WorkflowAction Create(
        Guid tenantId,
        Guid stepId,
        Guid instanceId,
        string actionType,
        string? payloadJson = null)
    {
        return new WorkflowAction
        {
            TenantId = tenantId,
            StepId = stepId,
            InstanceId = instanceId,
            ActionType = actionType,
            PayloadJson = payloadJson,
            Status = "Pending",
            ExecutedAt = DateTime.UtcNow,
        };
    }

    public void MarkSuccess()
    {
        Status = "Success";
    }

    public void MarkFailed(string error)
    {
        Status = "Failed";
        ErrorMessage = error;
    }
}
