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

    [JsonPropertyName("conditions")]
    public List<WorkflowConditionDefinition>? Conditions { get; set; }
}

public sealed class WorkflowConditionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("expression")]
    public string Expression { get; set; } = null!; // e.g. "context.Amount > 10000"

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class WorkflowStepDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = null!; // "action", "approval", "end", "parallel_gateway", "condition_gateway"

    [JsonPropertyName("transitions")]
    public List<WorkflowTransition> Transitions { get; set; } = [];

    [JsonPropertyName("actions")]
    public List<WorkflowActionDefinition>? Actions { get; set; }

    [JsonPropertyName("approverStrategy")]
    public string? ApproverStrategy { get; set; } // "supervisor", "role", "specific"

    [JsonPropertyName("approverLevels")]
    public int? ApproverLevels { get; set; } // multi-level: number of approval levels required

    [JsonPropertyName("approverRoleId")]
    public Guid? ApproverRoleId { get; set; } // for "role" strategy

    [JsonPropertyName("specificApproverIds")]
    public List<Guid>? SpecificApproverIds { get; set; } // for "specific" strategy

    [JsonPropertyName("parallelBranches")]
    public List<ParallelBranchDefinition>? ParallelBranches { get; set; }

    [JsonPropertyName("joinType")]
    public string? JoinType { get; set; } // "all" (wait for all branches) or "any" (first branch completes)

    [JsonPropertyName("convergenceStep")]
    public string? ConvergenceStep { get; set; } // step to advance to after parallel branches complete
}

public sealed class ParallelBranchDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("steps")]
    public List<string> Steps { get; set; } = []; // ordered step names in this branch

    [JsonPropertyName("condition")]
    public string? Condition { get; set; } // optional: only execute branch if condition is met
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
