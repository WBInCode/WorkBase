using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Forms.Domain.Entities;

public sealed class FormField : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid FormDefinitionId { get; private set; }
    public string Label { get; private set; } = null!;
    public string FieldType { get; private set; } = null!; // text, number, date, select, checkbox, textarea, file, email, phone
    public int Order { get; private set; }
    public bool IsRequired { get; private set; }
    public string? Placeholder { get; private set; }
    public string? ValidationRule { get; private set; } // regex or custom rule
    public string? OptionsJson { get; private set; } // for select/radio: JSON array of options
    public string? DefaultValue { get; private set; }

    private FormField() { }

    public static FormField Create(
        Guid formDefinitionId, Guid tenantId, string label, string fieldType, int order,
        bool isRequired = false, string? placeholder = null,
        string? validationRule = null, string? optionsJson = null, string? defaultValue = null)
    {
        return new FormField
        {
            FormDefinitionId = formDefinitionId,
            TenantId = tenantId,
            Label = label,
            FieldType = fieldType,
            Order = order,
            IsRequired = isRequired,
            Placeholder = placeholder,
            ValidationRule = validationRule,
            OptionsJson = optionsJson,
            DefaultValue = defaultValue,
        };
    }

    public void Update(string label, string fieldType, int order, bool isRequired,
        string? placeholder, string? validationRule, string? optionsJson, string? defaultValue)
    {
        Label = label;
        FieldType = fieldType;
        Order = order;
        IsRequired = isRequired;
        Placeholder = placeholder;
        ValidationRule = validationRule;
        OptionsJson = optionsJson;
        DefaultValue = defaultValue;
    }
}
