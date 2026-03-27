using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Domain.Entities;

public sealed class WorkflowDefinition : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string DefinitionJson { get; private set; } = null!;
    public int Version { get; private set; }
    public bool IsActive { get; private set; }

    private WorkflowDefinition() { }

    public static WorkflowDefinition Create(
        Guid tenantId,
        string name,
        string definitionJson,
        string? description = null)
    {
        return new WorkflowDefinition
        {
            TenantId = tenantId,
            Name = name,
            DefinitionJson = definitionJson,
            Description = description,
            Version = 1,
            IsActive = true,
        };
    }

    public void Update(string name, string definitionJson, string? description)
    {
        Name = name;
        DefinitionJson = definitionJson;
        Description = description;
        Version++;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
