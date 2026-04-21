using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Forms.Domain.Entities;

public sealed class FormSubmission : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid FormDefinitionId { get; private set; }
    public Guid? SubmittedBy { get; private set; }
    public string ValuesJson { get; private set; } = null!; // { "fieldId": "value", ... }
    public string Status { get; private set; } = null!; // Draft, Submitted, Approved, Rejected
    public Guid? WorkflowInstanceId { get; private set; }

    private FormSubmission() { }

    public static FormSubmission Create(
        Guid tenantId, Guid formDefinitionId, Guid? submittedBy, string valuesJson)
    {
        return new FormSubmission
        {
            TenantId = tenantId,
            FormDefinitionId = formDefinitionId,
            SubmittedBy = submittedBy,
            ValuesJson = valuesJson,
            Status = "Draft",
        };
    }

    public void Submit() => Status = "Submitted";
    public void Approve() => Status = "Approved";
    public void Reject() => Status = "Rejected";
    public void LinkWorkflow(Guid workflowInstanceId) => WorkflowInstanceId = workflowInstanceId;
    public void UpdateValues(string valuesJson) => ValuesJson = valuesJson;
}
