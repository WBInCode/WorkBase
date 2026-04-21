using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Forms.Domain.Entities;

public sealed class FormDefinition : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int Version { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsPublic { get; private set; }
    public string? WorkflowDefinitionName { get; private set; }

    private readonly List<FormField> _fields = [];
    public IReadOnlyCollection<FormField> Fields => _fields.AsReadOnly();

    private FormDefinition() { }

    public static FormDefinition Create(
        Guid tenantId, string name, string? description = null,
        bool isPublic = false, string? workflowDefinitionName = null)
    {
        return new FormDefinition
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            Version = 1,
            IsActive = true,
            IsPublic = isPublic,
            WorkflowDefinitionName = workflowDefinitionName,
        };
    }

    public void Update(string name, string? description, bool isPublic, string? workflowDefinitionName)
    {
        Name = name;
        Description = description;
        IsPublic = isPublic;
        WorkflowDefinitionName = workflowDefinitionName;
        Version++;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public FormField AddField(string label, string fieldType, int order,
        bool isRequired = false, string? placeholder = null,
        string? validationRule = null, string? optionsJson = null, string? defaultValue = null)
    {
        var field = FormField.Create(Id, TenantId, label, fieldType, order,
            isRequired, placeholder, validationRule, optionsJson, defaultValue);
        _fields.Add(field);
        return field;
    }
}
