using System.Text.Json.Serialization;

namespace WorkBase.Modules.Workflow.Application.Models;

/// <summary>
/// In-memory representation of a workflow definition parsed from JSON.
/// </summary>
public sealed class WorkflowDefinitionModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = null!;

    [JsonPropertyName("initialStep")]
    public string InitialStep { get; set; } = null!;

    [JsonPropertyName("steps")]
    public List<WorkflowStepDefinition> Steps { get; set; } = [];
}

public sealed class WorkflowStepDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = null!; // "action", "approval", "end"

    [JsonPropertyName("transitions")]
    public List<WorkflowTransition> Transitions { get; set; } = [];

    [JsonPropertyName("actions")]
    public List<WorkflowActionDefinition>? Actions { get; set; }

    [JsonPropertyName("approverStrategy")]
    public string? ApproverStrategy { get; set; } // "supervisor", "role", "specific"
}

public sealed class WorkflowTransition
{
    [JsonPropertyName("outcome")]
    public string Outcome { get; set; } = null!; // "approved", "rejected", "submitted", etc.

    [JsonPropertyName("targetStep")]
    public string TargetStep { get; set; } = null!;

    [JsonPropertyName("condition")]
    public string? Condition { get; set; }
}

public sealed class WorkflowActionDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!; // "notify", "create_task", "update_entity"

    [JsonPropertyName("trigger")]
    public string Trigger { get; set; } = null!; // "on_enter", "on_exit", "on_complete"

    [JsonPropertyName("payload")]
    public Dictionary<string, object>? Payload { get; set; }
}
