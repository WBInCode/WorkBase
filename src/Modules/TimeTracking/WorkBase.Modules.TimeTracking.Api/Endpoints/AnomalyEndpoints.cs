using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.TimeTracking.Application.Commands;
using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Modules.TimeTracking.Application.Queries;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.TimeTracking.Api.Endpoints;

public static class AnomalyEndpoints
{
    public static IEndpointRouteBuilder MapAnomalyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/time/anomalies")
            .WithTags("TimeTracking – Anomalies")
            .RequireAuthorization();

        group.MapGet("/", GetAnomalies)
            .WithName("GetAnomalies")
            .WithSummary("Pobierz listę anomalii za okres")
            .RequirePermission("time.view")
            .Produces<IReadOnlyList<TimeAnomalyDto>>();

        group.MapPut("/{id:guid}/review", ReviewAnomaly)
            .WithName("ReviewAnomaly")
            .WithSummary("Oznacz anomalię jako przejrzaną")
            .RequirePermission("time.manage")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/dismiss", DismissAnomaly)
            .WithName("DismissAnomaly")
            .WithSummary("Odrzuć anomalię")
            .RequirePermission("time.manage")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetAnomalies(
        [AsParameters] AnomalyQueryParams query,
        ISender sender)
    {
        var from = query.From ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var to = query.To ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await sender.Send(new GetAnomaliesQuery(from, to, query.Status));
        return result.ToHttpResult();
    }

    private static async Task<IResult> ReviewAnomaly(
        Guid id,
        HttpContext httpContext,
        ISender sender)
    {
        var userId = httpContext.User.FindFirstValue("sub") ?? "unknown";
        var command = new ReviewAnomalyCommand(id, userId);
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToHttpResult();
    }

    private static async Task<IResult> DismissAnomaly(
        Guid id,
        HttpContext httpContext,
        ISender sender)
    {
        var userId = httpContext.User.FindFirstValue("sub") ?? "unknown";
        var command = new DismissAnomalyCommand(id, userId);
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToHttpResult();
    }
}

public sealed record AnomalyQueryParams(DateOnly? From, DateOnly? To, string? Status);
