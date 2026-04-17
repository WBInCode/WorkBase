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

        // --- Cancel ---
        group.MapPost("/requests/{id:guid}/cancel", CancelLeaveRequest)
            .WithName("CancelLeaveRequest")
            .WithSummary("Anuluj wniosek urlopowy")
            .RequirePermission("leave.create");

        // --- Types CRUD ---
        group.MapPost("/types", CreateLeaveType)
            .WithName("CreateLeaveType")
            .WithSummary("Utwórz typ nieobecności")
            .RequirePermission("leave.manage");

        group.MapPut("/types/{id:guid}", UpdateLeaveType)
            .WithName("UpdateLeaveType")
            .WithSummary("Aktualizuj typ nieobecności")
            .RequirePermission("leave.manage");

        group.MapDelete("/types/{id:guid}", DeleteLeaveType)
            .WithName("DeleteLeaveType")
            .WithSummary("Usuń typ nieobecności")
            .RequirePermission("leave.manage")
            .Produces(StatusCodes.Status204NoContent);

        // --- Policies ---
        group.MapGet("/policies", GetLeavePolicies)
            .WithName("GetLeavePolicies")
            .WithSummary("Pobierz polityki urlopowe")
            .RequirePermission("leave.view")
            .Produces<List<LeavePolicyDto>>();

        group.MapPost("/policies", CreateLeavePolicy)
            .WithName("CreateLeavePolicy")
            .WithSummary("Utwórz politykę urlopową")
            .RequirePermission("leave.manage");

        group.MapPut("/policies/{id:guid}", UpdateLeavePolicy)
            .WithName("UpdateLeavePolicy")
            .WithSummary("Aktualizuj politykę urlopową")
            .RequirePermission("leave.manage");

        group.MapDelete("/policies/{id:guid}", DeleteLeavePolicy)
            .WithName("DeleteLeavePolicy")
            .WithSummary("Usuń politykę urlopową")
            .RequirePermission("leave.manage")
            .Produces(StatusCodes.Status204NoContent);

        // --- Conflict check ---
        group.MapPost("/conflicts/check", CheckLeaveConflict)
            .WithName("CheckLeaveConflict")
            .WithSummary("Sprawdź konflikt dat urlopowych")
            .RequirePermission("leave.view")
            .Produces<LeaveConflictResultDto>();

        // --- Balance adjust ---
        group.MapPost("/balances/{employeeId:guid}/adjust", AdjustLeaveBalance)
            .WithName("AdjustLeaveBalance")
            .WithSummary("Dostosuj saldo urlopowe")
            .RequirePermission("leave.manage");

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

    private static async Task<IResult> CancelLeaveRequest(Guid id, ISender sender)
    {
        var command = new CancelLeaveRequestCommand(id);
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateLeaveType(CreateLeaveTypeCommand command, ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateLeaveType(Guid id, UpdateLeaveTypeCommand command, ISender sender)
    {
        var result = await sender.Send(command with { LeaveTypeId = id });
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetLeavePolicies(ISender sender)
    {
        var query = new GetLeavePoliciesQuery();
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateLeavePolicy(CreateLeavePolicyCommand command, ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateLeavePolicy(Guid id, UpdateLeavePolicyCommand command, ISender sender)
    {
        var result = await sender.Send(command with { PolicyId = id });
        return result.ToHttpResult();
    }

    private static async Task<IResult> AdjustLeaveBalance(Guid employeeId, AdjustBalanceBody body, ISender sender)
    {
        var command = new AdjustLeaveBalanceCommand(employeeId, body.LeaveTypeId, body.Year, body.NewTotalDays);
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteLeaveType(Guid id, ISender sender)
    {
        var result = await sender.Send(new DeleteLeaveTypeCommand(id));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult> DeleteLeavePolicy(Guid id, ISender sender)
    {
        var result = await sender.Send(new DeleteLeavePolicyCommand(id));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult> CheckLeaveConflict(CheckConflictBody body, ISender sender)
    {
        var result = await sender.Send(new CheckLeaveConflictQuery(
            body.EmployeeId, body.StartDate, body.EndDate, body.ExcludeRequestId));
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

public sealed record AdjustBalanceBody(Guid LeaveTypeId, int Year, decimal NewTotalDays);

public sealed record CheckConflictBody(
    Guid EmployeeId, DateTime StartDate, DateTime EndDate, Guid? ExcludeRequestId = null);
