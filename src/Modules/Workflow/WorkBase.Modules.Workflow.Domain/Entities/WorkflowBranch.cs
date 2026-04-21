using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Domain.Entities;

public sealed class WorkflowBranch : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid InstanceId { get; private set; }
    public string GatewayStepName { get; private set; } = null!;
    public string BranchName { get; private set; } = null!;
    public string Status { get; private set; } = null!; // "Active", "Completed", "Skipped"
    public string? CurrentStepName { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private WorkflowBranch() { }

    public static WorkflowBranch Create(
        Guid tenantId, Guid instanceId, string gatewayStepName, string branchName, string initialStep)
    {
        return new WorkflowBranch
        {
            TenantId = tenantId,
            InstanceId = instanceId,
            GatewayStepName = gatewayStepName,
            BranchName = branchName,
            Status = "Active",
            CurrentStepName = initialStep,
            StartedAt = DateTime.UtcNow,
        };
    }

    public void AdvanceTo(string stepName) => CurrentStepName = stepName;

    public void Complete()
    {
        Status = "Completed";
        CompletedAt = DateTime.UtcNow;
    }

    public void Skip()
    {
        Status = "Skipped";
        CompletedAt = DateTime.UtcNow;
    }
}
