namespace WorkBase.Modules.AI.Domain.Entities;

public enum AiTaskType { Summarization, Classification, NextStep, SemanticSearch }
public enum AiTaskStatus { Pending, Processing, Completed, Failed }

public sealed class AiTaskLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = default!;
    public AiTaskType TaskType { get; set; }
    public string InputJson { get; set; } = "{}";
    public string? OutputJson { get; set; }
    public AiTaskStatus Status { get; set; }
    public int TokensUsed { get; set; }
    public string? ModelName { get; set; }
    public int LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
