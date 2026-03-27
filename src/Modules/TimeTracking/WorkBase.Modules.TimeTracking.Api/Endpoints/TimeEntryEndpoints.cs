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

public static class TimeEntryEndpoints
{
    public static IEndpointRouteBuilder MapTimeEntryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/time")
            .WithTags("TimeTracking")
            .RequireAuthorization();

        group.MapPost("/clock-in", ClockIn)
            .WithName("ClockIn")
            .WithSummary("Rejestracja wejścia (clock-in)")
            .RequirePermission("time.create")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/clock-out", ClockOut)
            .WithName("ClockOut")
            .WithSummary("Rejestracja wyjścia (clock-out)")
            .RequirePermission("time.create")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/break/start", StartBreak)
            .WithName("StartBreak")
            .WithSummary("Rozpoczęcie przerwy")
            .RequirePermission("time.create")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/break/end", EndBreak)
            .WithName("EndBreak")
            .WithSummary("Zakończenie przerwy")
            .RequirePermission("time.create")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/status/{employeeId:guid}", GetStatus)
            .WithName("GetTimeStatus")
            .WithSummary("Pobierz aktualny status czasu pracy pracownika")
            .RequirePermission("time.view")
            .Produces<TimeStatusDto>();

        group.MapGet("/timesheet/{employeeId:guid}", GetTimeSheet)
            .WithName("GetTimeSheet")
            .WithSummary("Pobierz kartę czasu pracy za okres (dzień/tydzień/miesiąc)")
            .RequirePermission("time.view")
            .Produces<TimeSheetPeriodDto>();

        return endpoints;
    }

    private static async Task<IResult> ClockIn(
        ClockInRequest request,
        ISender sender,
        HttpContext httpContext)
    {
        var command = new ClockInCommand(
            request.EmployeeId,
            request.Note,
            httpContext.Connection.RemoteIpAddress?.ToString());

        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/time/entries/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> ClockOut(
        ClockOutRequest request,
        ISender sender,
        HttpContext httpContext)
    {
        var command = new ClockOutCommand(
            request.EmployeeId,
            request.Note,
            httpContext.Connection.RemoteIpAddress?.ToString());

        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/time/entries/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> StartBreak(
        StartBreakRequest request,
        ISender sender)
    {
        var command = new StartBreakCommand(request.EmployeeId, request.Note);
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/time/entries/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> EndBreak(
        EndBreakRequest request,
        ISender sender)
    {
        var command = new EndBreakCommand(request.EmployeeId, request.Note);
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/time/entries/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> GetStatus(
        Guid employeeId,
        ISender sender)
    {
        var query = new GetCurrentStatusQuery(employeeId);
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetTimeSheet(
        Guid employeeId,
        [Microsoft.AspNetCore.Http.AsParameters] TimeSheetRequest request,
        ISender sender)
    {
        var from = request.From ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var to = request.To ?? from;
        var period = request.Period ?? "day";

        var query = new GetTimeSheetQuery(employeeId, from, to, period);
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }
}

public sealed record ClockInRequest(Guid EmployeeId, string? Note = null);
public sealed record ClockOutRequest(Guid EmployeeId, string? Note = null);
public sealed record StartBreakRequest(Guid EmployeeId, string? Note = null);
public sealed record EndBreakRequest(Guid EmployeeId, string? Note = null);
public sealed record TimeSheetRequest(DateOnly? From, DateOnly? To, string? Period);
