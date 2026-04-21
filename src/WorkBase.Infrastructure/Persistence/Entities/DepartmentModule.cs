using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Persistence.Entities;

public enum DepartmentModuleType { IT, HR, Logistics, Finance, Legal, Custom }

/// <summary>
/// Configurable department module — links an org unit to forms, workflows, and custom pages.
/// Table: cfg_department_modules
/// </summary>
public sealed class DepartmentModule : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrgUnitId { get; set; }
    public DepartmentModuleType ModuleType { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public string ConfigJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Links a form definition to a department module.
/// Table: cfg_department_module_forms
/// </summary>
public sealed class DepartmentModuleForm
{
    public Guid Id { get; set; }
    public Guid DepartmentModuleId { get; set; }
    public Guid FormDefinitionId { get; set; }
    public int SortOrder { get; set; }
    public string? Label { get; set; }
}

/// <summary>
/// Links a workflow definition to a department module.
/// Table: cfg_department_module_workflows
/// </summary>
public sealed class DepartmentModuleWorkflow
{
    public Guid Id { get; set; }
    public Guid DepartmentModuleId { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
    public int SortOrder { get; set; }
    public string? Label { get; set; }
}
