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

public static class ScheduleEndpoints
{
    public static IEndpointRouteBuilder MapScheduleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/time/schedules")
            .WithTags("TimeTracking – Schedules")
            .RequireAuthorization();

        group.MapGet("/{employeeId:guid}", GetSchedules)
            .WithName("GetSchedules")
            .WithSummary("Pobierz grafik pracownika za okres")
            .RequirePermission("time.view")
            .Produces<IReadOnlyList<ScheduleDto>>();

        group.MapPost("/", CreateSchedule)
            .WithName("CreateSchedule")
            .WithSummary("Utwórz wpis grafiku")
            .RequirePermission("time.manage")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateSchedule)
            .WithName("UpdateSchedule")
            .WithSummary("Zaktualizuj wpis grafiku")
            .RequirePermission("time.manage")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteSchedule)
            .WithName("DeleteSchedule")
            .WithSummary("Usuń wpis grafiku")
            .RequirePermission("time.manage")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // Schedule Templates
        var templates = endpoints.MapGroup("/api/time/schedule-templates")
            .WithTags("TimeTracking – Schedule Templates")
            .RequireAuthorization();

        templates.MapGet("/", GetTemplates)
            .WithName("GetScheduleTemplates")
            .WithSummary("Pobierz szablony grafiku")
            .RequirePermission("time.view")
            .Produces<IReadOnlyList<ScheduleTemplateDto>>();

        templates.MapPost("/", CreateTemplate)
            .WithName("CreateScheduleTemplate")
            .WithSummary("Utwórz szablon grafiku")
            .RequirePermission("time.manage")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        templates.MapPut("/{id:guid}", UpdateTemplate)
            .WithName("UpdateScheduleTemplate")
            .WithSummary("Zaktualizuj szablon grafiku")
            .RequirePermission("time.manage")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetSchedules(
        Guid employeeId,
        [AsParameters] ScheduleQueryParams query,
        ISender sender)
    {
        var from = query.From ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var to = query.To ?? from.AddDays(6);

        var result = await sender.Send(new GetSchedulesQuery(employeeId, from, to));
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateSchedule(
        CreateScheduleRequest request,
        ISender sender)
    {
        var command = new CreateScheduleCommand(
            request.EmployeeId,
            request.Date,
            request.PlannedStart,
            request.PlannedEnd,
            request.ShiftType,
            request.TemplateId);

        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/time/schedules/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdateSchedule(
        Guid id,
        UpdateScheduleRequest request,
        ISender sender)
    {
        var command = new UpdateScheduleCommand(id, request.PlannedStart, request.PlannedEnd, request.ShiftType);
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToHttpResult();
    }

    private static async Task<IResult> DeleteSchedule(
        Guid id,
        ISender sender)
    {
        var result = await sender.Send(new DeleteScheduleCommand(id));

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToHttpResult();
    }

    private static async Task<IResult> GetTemplates(ISender sender)
    {
        var result = await sender.Send(new GetScheduleTemplatesQuery());
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateTemplate(
        CreateScheduleTemplateRequest request,
        ISender sender)
    {
        var command = new CreateScheduleTemplateCommand(request.Name, request.Definition, request.Description);
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/time/schedule-templates/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdateTemplate(
        Guid id,
        UpdateScheduleTemplateRequest request,
        ISender sender)
    {
        var command = new UpdateScheduleTemplateCommand(id, request.Name, request.Definition, request.Description);
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToHttpResult();
    }
}

public sealed record ScheduleQueryParams(DateOnly? From, DateOnly? To);

public sealed record CreateScheduleRequest(
    Guid EmployeeId,
    DateOnly Date,
    TimeOnly PlannedStart,
    TimeOnly PlannedEnd,
    string? ShiftType = null,
    Guid? TemplateId = null);

public sealed record UpdateScheduleRequest(
    TimeOnly PlannedStart,
    TimeOnly PlannedEnd,
    string? ShiftType = null);

public sealed record CreateScheduleTemplateRequest(
    string Name,
    string Definition,
    string? Description = null);

public sealed record UpdateScheduleTemplateRequest(
    string Name,
    string Definition,
    string? Description = null);
