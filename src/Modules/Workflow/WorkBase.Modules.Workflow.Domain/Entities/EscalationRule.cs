using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Domain.Entities;

public sealed class EscalationRule : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid DefinitionId { get; private set; }
    public string StepName { get; private set; } = null!;
    public int TimeoutMinutes { get; private set; }
    public string ActionType { get; private set; } = null!;
    public string? ActionPayloadJson { get; private set; }
    public bool IsActive { get; private set; }

    private EscalationRule() { }

    public static EscalationRule Create(
        Guid tenantId,
        Guid definitionId,
        string stepName,
        int timeoutMinutes,
        string actionType,
        string? actionPayloadJson = null)
    {
        return new EscalationRule
        {
            TenantId = tenantId,
            DefinitionId = definitionId,
            StepName = stepName,
            TimeoutMinutes = timeoutMinutes,
            ActionType = actionType,
            ActionPayloadJson = actionPayloadJson,
            IsActive = true,
        };
    }

    public void Update(int timeoutMinutes, string actionType, string? actionPayloadJson)
    {
        TimeoutMinutes = timeoutMinutes;
        ActionType = actionType;
        ActionPayloadJson = actionPayloadJson;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
