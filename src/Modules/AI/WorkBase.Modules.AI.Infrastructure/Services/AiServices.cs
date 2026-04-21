using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkBase.Modules.AI.Application.Contracts;

namespace WorkBase.Modules.AI.Infrastructure.Services;

public sealed class OpenAiCompletionService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<OpenAiCompletionService> logger) : IAiCompletionService
{
    public async Task<AiCompletionResult> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var apiKey = configuration["AI:OpenAI:ApiKey"];
        var model = configuration["AI:OpenAI:Model"] ?? "gpt-4o-mini";
        var baseUrl = configuration["AI:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";

        var client = httpClientFactory.CreateClient("OpenAI");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var body = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_tokens = 1024,
            temperature = 0.3
        };

        var sw = Stopwatch.StartNew();
        var response = await client.PostAsJsonAsync($"{baseUrl}/chat/completions", body, ct);
        sw.Stop();

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        var content = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        var tokensUsed = json.TryGetProperty("usage", out var usage)
            ? usage.GetProperty("total_tokens").GetInt32()
            : 0;

        logger.LogInformation("AI completion: model={Model} tokens={Tokens} latency={Latency}ms", model, tokensUsed, sw.ElapsedMilliseconds);

        return new AiCompletionResult(content, tokensUsed, model, (int)sw.ElapsedMilliseconds);
    }
}

public sealed class OpenAiEmbeddingService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<OpenAiEmbeddingService> logger) : IAiEmbeddingService
{
    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct)
    {
        var apiKey = configuration["AI:OpenAI:ApiKey"];
        var model = configuration["AI:OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        var baseUrl = configuration["AI:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";

        var client = httpClientFactory.CreateClient("OpenAI");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var body = new { model, input = text };
        var response = await client.PostAsJsonAsync($"{baseUrl}/embeddings", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var embedding = json.GetProperty("data")[0].GetProperty("embedding");
        var result = new float[embedding.GetArrayLength()];
        var i = 0;
        foreach (var val in embedding.EnumerateArray())
            result[i++] = val.GetSingle();

        logger.LogInformation("AI embedding: {Length} dimensions", result.Length);
        return result;
    }

    public Task<List<AiSearchResult>> SemanticSearchAsync(Guid tenantId, string query, int maxResults, CancellationToken ct)
    {
        // Placeholder: In production, this would query a vector DB (pgvector / Qdrant / Pinecone)
        // embedding = GetEmbeddingAsync(query) → vector similarity search → return ranked results
        logger.LogInformation("Semantic search: tenant={TenantId} query={Query} maxResults={Max}", tenantId, query, maxResults);
        return Task.FromResult(new List<AiSearchResult>());
    }
}
