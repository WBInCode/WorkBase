using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.TimeTracking.Application.Commands;
using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Modules.TimeTracking.Application.Queries;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.TimeTracking.Api.Endpoints;

public static class BreakPolicyEndpoints
{
    public static IEndpointRouteBuilder MapBreakPolicyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/time/break-policies")
            .WithTags("BreakPolicies")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetBreakPolicies")
            .WithSummary("Pobierz wszystkie polityki przerw")
            .RequirePermission("time.view")
            .Produces<List<BreakPolicyDto>>();

        group.MapPost("/", Create)
            .WithName("CreateBreakPolicy")
            .WithSummary("Utwórz politykę przerw")
            .RequirePermission("time.manage")
            .Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateBreakPolicy")
            .WithSummary("Aktualizuj politykę przerw")
            .RequirePermission("time.manage")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteBreakPolicy")
            .WithSummary("Usuń politykę przerw")
            .RequirePermission("time.manage")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetAll(ISender sender)
    {
        var query = new GetBreakPoliciesQuery();
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Create(
        CreateBreakPolicyRequest request,
        ISender sender)
    {
        if (!Enum.TryParse<BreakType>(request.BreakType, true, out var breakType))
            return Results.BadRequest("Nieprawidłowy typ przerwy. Dozwolone: Paid, Unpaid.");

        var command = new CreateBreakPolicyCommand(
            request.Name,
            breakType,
            request.MaxPerDay,
            request.MaxMinutesPerBreak,
            request.MaxMinutesPerDay,
            request.IsActive);

        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/time/break-policies/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateBreakPolicyRequest request,
        ISender sender)
    {
        var command = new UpdateBreakPolicyCommand(
            id,
            request.Name,
            request.MaxPerDay,
            request.MaxMinutesPerBreak,
            request.MaxMinutesPerDay,
            request.IsActive);

        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToHttpResult();
    }

    private static async Task<IResult> Delete(Guid id, ISender sender)
    {
        var command = new DeleteBreakPolicyCommand(id);
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToHttpResult();
    }
}

public sealed record CreateBreakPolicyRequest(
    string Name,
    string BreakType,
    int? MaxPerDay,
    int? MaxMinutesPerBreak,
    int? MaxMinutesPerDay,
    bool IsActive = true);

public sealed record UpdateBreakPolicyRequest(
    string Name,
    int? MaxPerDay,
    int? MaxMinutesPerBreak,
    int? MaxMinutesPerDay,
    bool IsActive);
