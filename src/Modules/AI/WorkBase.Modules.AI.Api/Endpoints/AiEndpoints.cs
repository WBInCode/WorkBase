using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.AI.Application.Commands;
using WorkBase.Modules.AI.Application.Contracts;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.AI.Api.Endpoints;

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/ai").WithTags("AI").RequireAuthorization();

        group.MapPost("/summarize", async (SummarizeRequest req, ISender sender) =>
        {
            var result = await sender.Send(new SummarizeCommand(req.EntityType, req.EntityId, req.Content));
            return result.IsSuccess ? Results.Ok(new { Summary = result.Value }) : result.ToHttpResult();
        }).WithName("AiSummarize").WithSummary("AI podsumowanie encji").RequirePermission("ai.use");

        group.MapPost("/classify", async (ClassifyRequest req, ISender sender) =>
        {
            var result = await sender.Send(new ClassifyCommand(req.Content, req.Categories));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
        }).WithName("AiClassify").WithSummary("AI klasyfikacja tekstu").RequirePermission("ai.use");

        group.MapPost("/suggest-next-step", async (SuggestNextStepRequest req, ISender sender) =>
        {
            var result = await sender.Send(new SuggestNextStepCommand(req.EntityType, req.EntityId, req.Context));
            return result.IsSuccess ? Results.Ok(new { Suggestion = result.Value }) : result.ToHttpResult();
        }).WithName("AiSuggestNextStep").WithSummary("AI sugestia następnego kroku").RequirePermission("ai.use");

        group.MapPost("/search", async (SemanticSearchRequest req, ISender sender) =>
        {
            var result = await sender.Send(new SemanticSearchQuery(req.Query, req.MaxResults));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
        }).WithName("AiSemanticSearch").WithSummary("AI wyszukiwanie semantyczne").RequirePermission("ai.use");

        return endpoints;
    }
}

public sealed record SummarizeRequest(string EntityType, string EntityId, string Content);
public sealed record ClassifyRequest(string Content, List<string> Categories);
public sealed record SuggestNextStepRequest(string EntityType, string EntityId, string Context);
public sealed record SemanticSearchRequest(string Query, int MaxResults = 10);
