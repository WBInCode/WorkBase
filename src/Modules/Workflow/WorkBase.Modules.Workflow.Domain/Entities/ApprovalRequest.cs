using WorkBase.Modules.Workflow.Domain.Events;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Domain.Entities;

public sealed class ApprovalRequest : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid StepId { get; private set; }
    public Guid InstanceId { get; private set; }
    public Guid RequesterId { get; private set; }
    public Guid ApproverId { get; private set; }
    public string Status { get; private set; } = null!;
    public DateTime? DueDate { get; private set; }
    public int Order { get; private set; }

    private ApprovalRequest() { }

    public static ApprovalRequest Create(
        Guid tenantId,
        Guid stepId,
        Guid instanceId,
        Guid requesterId,
        Guid approverId,
        DateTime? dueDate = null,
        int order = 0)
    {
        var request = new ApprovalRequest
        {
            TenantId = tenantId,
            StepId = stepId,
            InstanceId = instanceId,
            RequesterId = requesterId,
            ApproverId = approverId,
            Status = "Pending",
            DueDate = dueDate,
            Order = order,
        };

        request.RaiseDomainEvent(new ApprovalRequestCreatedEvent(
            request.Id, tenantId, instanceId, stepId, requesterId, approverId));

        return request;
    }

    public void Approve()
    {
        Status = "Approved";
    }

    public void Reject()
    {
        Status = "Rejected";
    }

    public void Return()
    {
        Status = "Returned";
    }
}
