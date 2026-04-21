namespace WorkBase.Modules.AI.Application.Contracts;

public interface IAiCompletionService
{
    Task<AiCompletionResult> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}

public sealed record AiCompletionResult(string Content, int TokensUsed, string ModelName, int LatencyMs);

public interface IAiEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
    Task<List<AiSearchResult>> SemanticSearchAsync(Guid tenantId, string query, int maxResults = 10, CancellationToken ct = default);
}

public sealed record AiSearchResult(string EntityType, string EntityId, string Title, string Snippet, double Score);

public interface IAiTaskLogRepository
{
    Task AddAsync(Domain.Entities.AiTaskLog log, CancellationToken ct = default);
    Task<List<Domain.Entities.AiTaskLog>> GetRecentAsync(Guid tenantId, int count = 20, CancellationToken ct = default);
}
