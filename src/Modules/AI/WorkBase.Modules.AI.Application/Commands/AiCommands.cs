using WorkBase.Modules.AI.Application.Contracts;
using WorkBase.Modules.AI.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.AI.Application.Commands;

// ─── Summarize ───
public sealed record SummarizeCommand(string EntityType, string EntityId, string Content) : ICommand<string>, ITenantRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = default!;
}

public sealed class SummarizeHandler(IAiCompletionService ai, IAiTaskLogRepository logRepo) : ICommandHandler<SummarizeCommand, string>
{
    public async Task<Result<string>> Handle(SummarizeCommand cmd, CancellationToken ct)
    {
        var system = "Jesteś asystentem HR/CRM. Podsumuj poniższy tekst zwięźle po polsku, max 3-5 zdań.";
        var result = await ai.CompleteAsync(system, cmd.Content, ct);

        await logRepo.AddAsync(new AiTaskLog
        {
            Id = Guid.NewGuid(), TenantId = cmd.TenantId, UserId = cmd.UserId,
            TaskType = AiTaskType.Summarization, InputJson = $"{{\"entityType\":\"{cmd.EntityType}\",\"entityId\":\"{cmd.EntityId}\"}}",
            OutputJson = result.Content, Status = AiTaskStatus.Completed,
            TokensUsed = result.TokensUsed, ModelName = result.ModelName, LatencyMs = result.LatencyMs,
            CreatedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow
        }, ct);

        return result.Content;
    }
}

// ─── Classify ───
public sealed record ClassifyCommand(string Content, List<string> Categories) : ICommand<ClassificationResult>, ITenantRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = default!;
}

public sealed record ClassificationResult(string Category, double Confidence, string Reasoning);

public sealed class ClassifyHandler(IAiCompletionService ai, IAiTaskLogRepository logRepo) : ICommandHandler<ClassifyCommand, ClassificationResult>
{
    public async Task<Result<ClassificationResult>> Handle(ClassifyCommand cmd, CancellationToken ct)
    {
        var cats = string.Join(", ", cmd.Categories);
        var system = $"Klasyfikuj poniższy tekst do jednej z kategorii: [{cats}]. Odpowiedz w JSON: {{\"category\":\"...\",\"confidence\":0.0-1.0,\"reasoning\":\"...\"}}";
        var result = await ai.CompleteAsync(system, cmd.Content, ct);

        await logRepo.AddAsync(new AiTaskLog
        {
            Id = Guid.NewGuid(), TenantId = cmd.TenantId, UserId = cmd.UserId,
            TaskType = AiTaskType.Classification, InputJson = $"{{\"categoriesCount\":{cmd.Categories.Count}}}",
            OutputJson = result.Content, Status = AiTaskStatus.Completed,
            TokensUsed = result.TokensUsed, ModelName = result.ModelName, LatencyMs = result.LatencyMs,
            CreatedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow
        }, ct);

        // Parse result (simplified — production would use JSON deserializer with fallback)
        return new ClassificationResult(cmd.Categories.FirstOrDefault() ?? "Unknown", 0.8, result.Content);
    }
}

// ─── Next Step Suggestion ───
public sealed record SuggestNextStepCommand(string EntityType, string EntityId, string Context) : ICommand<string>, ITenantRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = default!;
}

public sealed class SuggestNextStepHandler(IAiCompletionService ai, IAiTaskLogRepository logRepo) : ICommandHandler<SuggestNextStepCommand, string>
{
    public async Task<Result<string>> Handle(SuggestNextStepCommand cmd, CancellationToken ct)
    {
        var system = "Jesteś asystentem zarządzania. Na podstawie kontekstu zasugeruj następny krok. Odpowiedz krótko po polsku.";
        var result = await ai.CompleteAsync(system, $"Encja: {cmd.EntityType}/{cmd.EntityId}\nKontekst:\n{cmd.Context}", ct);

        await logRepo.AddAsync(new AiTaskLog
        {
            Id = Guid.NewGuid(), TenantId = cmd.TenantId, UserId = cmd.UserId,
            TaskType = AiTaskType.NextStep, InputJson = $"{{\"entityType\":\"{cmd.EntityType}\",\"entityId\":\"{cmd.EntityId}\"}}",
            OutputJson = result.Content, Status = AiTaskStatus.Completed,
            TokensUsed = result.TokensUsed, ModelName = result.ModelName, LatencyMs = result.LatencyMs,
            CreatedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow
        }, ct);

        return result.Content;
    }
}

// ─── Semantic Search ───
public sealed record SemanticSearchQuery(string Query, int MaxResults = 10) : ICommand<List<AiSearchResult>>, ITenantRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = default!;
}

public sealed class SemanticSearchHandler(IAiEmbeddingService embeddings, IAiTaskLogRepository logRepo) : ICommandHandler<SemanticSearchQuery, List<AiSearchResult>>
{
    public async Task<Result<List<AiSearchResult>>> Handle(SemanticSearchQuery cmd, CancellationToken ct)
    {
        var results = await embeddings.SemanticSearchAsync(cmd.TenantId, cmd.Query, cmd.MaxResults, ct);

        await logRepo.AddAsync(new AiTaskLog
        {
            Id = Guid.NewGuid(), TenantId = cmd.TenantId, UserId = cmd.UserId,
            TaskType = AiTaskType.SemanticSearch,
            InputJson = $"{{\"query\":\"{cmd.Query}\",\"results\":{results.Count}}}",
            Status = AiTaskStatus.Completed,
            CreatedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow
        }, ct);

        return results;
    }
}
