using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Domain.Entities;

public sealed class WorkflowStep : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid InstanceId { get; private set; }
    public string StepName { get; private set; } = null!;
    public string Status { get; private set; } = null!;
    public DateTime? EnteredAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? CompletedBy { get; private set; }
    public string? Outcome { get; private set; }
    public string? Comment { get; private set; }

    private WorkflowStep() { }

    public static WorkflowStep Create(
        Guid tenantId,
        Guid instanceId,
        string stepName)
    {
        return new WorkflowStep
        {
            TenantId = tenantId,
            InstanceId = instanceId,
            StepName = stepName,
            Status = "Active",
            EnteredAt = DateTime.UtcNow,
        };
    }

    public void Complete(string outcome, string? completedBy = null, string? comment = null)
    {
        Status = "Completed";
        Outcome = outcome;
        CompletedAt = DateTime.UtcNow;
        CompletedBy = completedBy;
        Comment = comment;
    }

    public void Skip()
    {
        Status = "Skipped";
        CompletedAt = DateTime.UtcNow;
    }
}
