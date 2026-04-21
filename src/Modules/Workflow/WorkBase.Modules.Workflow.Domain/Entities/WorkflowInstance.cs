using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Domain.Entities;

public sealed class WorkflowInstance : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid DefinitionId { get; private set; }
    public string EntityType { get; private set; } = null!;
    public Guid EntityId { get; private set; }
    public string CurrentStepName { get; private set; } = null!;
    public string Status { get; private set; } = null!;
    public Guid InitiatedBy { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ContextJson { get; private set; }

    private WorkflowInstance() { }

    public static WorkflowInstance Create(
        Guid tenantId,
        Guid definitionId,
        string entityType,
        Guid entityId,
        string initialStepName,
        Guid initiatedBy)
    {
        return new WorkflowInstance
        {
            TenantId = tenantId,
            DefinitionId = definitionId,
            EntityType = entityType,
            EntityId = entityId,
            CurrentStepName = initialStepName,
            Status = "Active",
            InitiatedBy = initiatedBy,
        };
    }

    public void AdvanceTo(string stepName)
    {
        CurrentStepName = stepName;
    }

    public void SetContext(string contextJson)
    {
        ContextJson = contextJson;
    }

    public void Complete()
    {
        Status = "Completed";
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = "Cancelled";
        CompletedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = "Rejected";
        CompletedAt = DateTime.UtcNow;
    }
}
