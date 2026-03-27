using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Domain.Entities;

public sealed class ApprovalRequest : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid StepId { get; private set; }
    public Guid InstanceId { get; private set; }
    public Guid ApproverId { get; private set; }
    public string Status { get; private set; } = null!;
    public DateTime? DueDate { get; private set; }
    public int Order { get; private set; }

    private ApprovalRequest() { }

    public static ApprovalRequest Create(
        Guid tenantId,
        Guid stepId,
        Guid instanceId,
        Guid approverId,
        DateTime? dueDate = null,
        int order = 0)
    {
        return new ApprovalRequest
        {
            TenantId = tenantId,
            StepId = stepId,
            InstanceId = instanceId,
            ApproverId = approverId,
            Status = "Pending",
            DueDate = dueDate,
            Order = order,
        };
    }

    public void MarkDecided(string status)
    {
        Status = status;
    }
}
