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

public static class OrgUnitScheduleEndpoints
{
    public static IEndpointRouteBuilder MapOrgUnitScheduleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/time/org-unit-schedules")
            .WithTags("TimeTracking – Org Unit Schedules")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetOrgUnitSchedules")
            .WithSummary("Pobierz wszystkie grafiki jednostek organizacyjnych")
            .RequirePermission("time.view")
            .Produces<IReadOnlyList<OrgUnitScheduleDto>>();

        group.MapGet("/{orgUnitId:guid}", GetByOrgUnit)
            .WithName("GetOrgUnitSchedule")
            .WithSummary("Pobierz grafik jednostki organizacyjnej (z dziedziczeniem)")
            .RequirePermission("time.view")
            .Produces<OrgUnitScheduleDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateOrgUnitSchedule")
            .WithSummary("Utwórz grafik jednostki organizacyjnej")
            .RequirePermission("time.manage")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateOrgUnitSchedule")
            .WithSummary("Zaktualizuj grafik jednostki (regeneruje przyszłe wpisy)")
            .RequirePermission("time.manage")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteOrgUnitSchedule")
            .WithSummary("Usuń grafik jednostki (usuwa przyszłe wygenerowane wpisy)")
            .RequirePermission("time.manage")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetAll(ISender sender)
    {
        var result = await sender.Send(new GetOrgUnitSchedulesQuery());
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetByOrgUnit(Guid orgUnitId, ISender sender)
    {
        var result = await sender.Send(new GetOrgUnitScheduleQuery(orgUnitId));
        if (result.IsSuccess && result.Value is null)
            return Results.NotFound();
        return result.ToHttpResult();
    }

    private static async Task<IResult> Create(CreateOrgUnitScheduleRequest request, ISender sender)
    {
        var command = new CreateOrgUnitScheduleCommand(
            request.OrgUnitId, request.Name, request.WeekPattern, request.EffectiveFrom);
        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Created($"/api/time/org-unit-schedules/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> Update(Guid id, UpdateOrgUnitScheduleRequest request, ISender sender)
    {
        var command = new UpdateOrgUnitScheduleCommand(id, request.Name, request.WeekPattern, request.EffectiveFrom);
        var result = await sender.Send(command);
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult> Delete(Guid id, ISender sender)
    {
        var result = await sender.Send(new DeleteOrgUnitScheduleCommand(id));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }
}

public sealed record CreateOrgUnitScheduleRequest(
    Guid OrgUnitId,
    string Name,
    string WeekPattern,
    DateOnly EffectiveFrom);

public sealed record UpdateOrgUnitScheduleRequest(
    string Name,
    string WeekPattern,
    DateOnly EffectiveFrom);
