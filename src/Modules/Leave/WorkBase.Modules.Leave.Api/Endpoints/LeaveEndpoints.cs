using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Leave.Application.Commands;
using WorkBase.Modules.Leave.Application.Dtos;
using WorkBase.Modules.Leave.Application.Queries;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Leave.Api.Endpoints;

public static class LeaveEndpoints
{
    public static IEndpointRouteBuilder MapLeaveEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/leave")
            .WithTags("Leave")
            .RequireAuthorization();

        // --- Types ---
        group.MapGet("/types", GetLeaveTypes)
            .WithName("GetLeaveTypes")
            .WithSummary("Pobierz aktywne typy nieobecności")
            .RequirePermission("leave.view")
            .Produces<List<LeaveTypeDto>>();

        // --- Balances ---
        group.MapGet("/balances/{employeeId:guid}", GetLeaveBalances)
            .WithName("GetLeaveBalances")
            .WithSummary("Pobierz saldo urlopowe pracownika")
            .RequirePermission("leave.view")
            .Produces<List<LeaveBalanceDto>>();

        // --- Requests ---
        group.MapGet("/requests/{employeeId:guid}", GetLeaveRequests)
            .WithName("GetLeaveRequests")
            .WithSummary("Pobierz wnioski urlopowe pracownika")
            .RequirePermission("leave.view")
            .Produces<List<LeaveRequestDto>>();

        group.MapPost("/requests", SubmitLeaveRequest)
            .WithName("SubmitLeaveRequest")
            .WithSummary("Złóż wniosek urlopowy")
            .RequirePermission("leave.create")
            .Produces<Guid>(StatusCodes.Status201Created);

        // --- Calendar ---
        group.MapPost("/calendar", GetLeaveCalendar)
            .WithName("GetLeaveCalendar")
            .WithSummary("Pobierz kalendarz nieobecności zespołu")
            .RequirePermission("leave.view")
            .Produces<List<LeaveCalendarEntryDto>>();

        return endpoints;
    }

    private static async Task<IResult> GetLeaveTypes(ISender sender)
    {
        var query = new GetLeaveTypesQuery();
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetLeaveBalances(
        Guid employeeId, int? year, ISender sender)
    {
        var query = new GetLeaveBalancesQuery(employeeId, year ?? DateTime.UtcNow.Year);
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetLeaveRequests(
        Guid employeeId, int? year, ISender sender)
    {
        var query = new GetLeaveRequestsQuery(employeeId, year);
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    private static async Task<IResult> SubmitLeaveRequest(
        SubmitLeaveRequestBody body, ISender sender)
    {
        var command = new SubmitLeaveRequestCommand(
            body.EmployeeId,
            body.LeaveTypeId,
            body.StartDate,
            body.EndDate,
            body.TotalDays,
            body.Reason);

        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/leave/requests/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> GetLeaveCalendar(
        LeaveCalendarRequestBody body, ISender sender)
    {
        var query = new GetLeaveCalendarQuery(body.EmployeeIds, body.From, body.To);
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }
}

public sealed record SubmitLeaveRequestBody(
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalDays,
    string? Reason = null);

public sealed record LeaveCalendarRequestBody(
    List<Guid> EmployeeIds,
    DateTime From,
    DateTime To);
