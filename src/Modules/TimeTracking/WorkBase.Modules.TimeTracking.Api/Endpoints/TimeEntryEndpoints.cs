using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using WorkBase.Contracts;
using WorkBase.Modules.TimeTracking.Application.Commands;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Modules.TimeTracking.Application.Queries;
using WorkBase.Modules.TimeTracking.Domain.Entities;
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

        group.MapGet("/break-availability/{employeeId:guid}", GetBreakAvailability)
            .WithName("GetBreakAvailability")
            .WithSummary("Pobierz dostępność przerw dla pracownika")
            .RequirePermission("time.view")
            .Produces<BreakAvailabilityDto>();

        group.MapGet("/timesheet/{employeeId:guid}", GetTimeSheet)
            .WithName("GetTimeSheet")
            .WithSummary("Pobierz kartę czasu pracy za okres (dzień/tydzień/miesiąc)")
            .RequirePermission("time.view")
            .Produces<TimeSheetPeriodDto>();

        // Uzupelnianie ewidencji: HR/Admin (time.manage) dla calej firmy, kierownik (time.edit)
        // wylacznie dla siebie i swojego zespolu — zakres pilnuje EnsureCanManage.
        group.MapPost("/entries", AdminCreateEntry)
            .WithName("AdminCreateTimeEntry")
            .WithSummary("Dodaj wpis czasu pracy pracownika")
            .RequireAnyPermission("time.manage", "time.edit")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/entries/{entryId:guid}", AdminUpdateEntry)
            .WithName("AdminUpdateTimeEntry")
            .WithSummary("Edytuj wpis czasu pracy")
            .RequireAnyPermission("time.manage", "time.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/entries/{entryId:guid}", AdminDeleteEntry)
            .WithName("AdminDeleteTimeEntry")
            .WithSummary("Usun wpis czasu pracy")
            .RequireAnyPermission("time.manage", "time.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

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
        if (!Enum.TryParse<BreakType>(request.BreakType, true, out var breakType))
            return Results.BadRequest("Nieprawidłowy typ przerwy. Dozwolone: Paid, Unpaid.");

        var command = new StartBreakCommand(request.EmployeeId, breakType, request.Note);
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
        ClaimsPrincipal user,
        IPermissionService permissions,
        IEmployeeScopeResolver scopes,
        ISender sender,
        CancellationToken ct)
    {
        // Samo uprawnienie time.view ma kazdy pracownik, wiec bez sprawdzenia zakresu
        // wystarczylo podmienic identyfikator w adresie, zeby zobaczyc dane kolegi.
        if (!await user.CanAccessEmployeeAsync(employeeId, permissions, scopes, "time.view-team", "time", ct))
            return Results.Forbid();

        var query = new GetCurrentStatusQuery(employeeId);
        var result = await sender.Send(query, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetBreakAvailability(
        Guid employeeId,
        ClaimsPrincipal user,
        IPermissionService permissions,
        IEmployeeScopeResolver scopes,
        ISender sender,
        CancellationToken ct)
    {
        if (!await user.CanAccessEmployeeAsync(employeeId, permissions, scopes, "time.view-team", "time", ct))
            return Results.Forbid();

        var query = new GetBreakAvailabilityQuery(employeeId);
        var result = await sender.Send(query, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetTimeSheet(
        Guid employeeId,
        [Microsoft.AspNetCore.Http.AsParameters] TimeSheetRequest request,
        ClaimsPrincipal user,
        IPermissionService permissions,
        IEmployeeScopeResolver scopes,
        ISender sender,
        CancellationToken ct)
    {
        if (!await user.CanAccessEmployeeAsync(employeeId, permissions, scopes, "time.view-team", "time", ct))
            return Results.Forbid();

        var from = request.From ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var to = request.To ?? from;
        var period = request.Period ?? "day";

        var query = new GetTimeSheetQuery(employeeId, from, to, period);
        var result = await sender.Send(query, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> AdminCreateEntry(
        AdminCreateTimeEntryRequest request,
        ClaimsPrincipal caller,
        ITimeManagementScopeService scope,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureCanManage(caller, scope, request.EmployeeId, cancellationToken);
        if (denied is not null)
            return denied;

        var command = new AdminCreateTimeEntryCommand(
            request.EmployeeId,
            request.EntryTime,
            request.Type,
            request.BreakType,
            request.Note);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/time/entries/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> AdminUpdateEntry(
        Guid entryId,
        AdminUpdateTimeEntryRequest request,
        ClaimsPrincipal caller,
        ITimeManagementScopeService scope,
        ITimeEntryRepository entries,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureCanManageEntry(caller, scope, entries, entryId, cancellationToken);
        if (denied is not null)
            return denied;

        var command = new AdminUpdateTimeEntryCommand(
            entryId,
            request.EntryTime,
            request.Type,
            request.BreakType,
            request.Note);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToHttpResult();
    }

    private static async Task<IResult> AdminDeleteEntry(
        Guid entryId,
        ClaimsPrincipal caller,
        ITimeManagementScopeService scope,
        ITimeEntryRepository entries,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureCanManageEntry(caller, scope, entries, entryId, cancellationToken);
        if (denied is not null)
            return denied;

        var result = await sender.Send(new AdminDeleteTimeEntryCommand(entryId), cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.ToHttpResult();
    }

    private static async Task<IResult?> EnsureCanManageEntry(
        ClaimsPrincipal caller,
        ITimeManagementScopeService scope,
        ITimeEntryRepository entries,
        Guid entryId,
        CancellationToken cancellationToken)
    {
        var tenantId = PermissionClaims.GetTenantId(caller);
        if (tenantId is null)
            return Results.Forbid();

        var entry = await entries.GetByIdAsync(tenantId.Value, entryId, cancellationToken);
        if (entry is null)
            return Results.NotFound();

        return await EnsureCanManage(caller, scope, entry.EmployeeId, cancellationToken);
    }

    private static async Task<IResult?> EnsureCanManage(
        ClaimsPrincipal caller,
        ITimeManagementScopeService scope,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var userId = PermissionClaims.GetUserId(caller);
        var tenantId = PermissionClaims.GetTenantId(caller);
        if (userId is null || tenantId is null)
            return Results.Forbid();

        var allowed = await scope.CanManageEmployeeTimeAsync(
            userId.Value, tenantId.Value, employeeId, cancellationToken);

        return allowed
            ? null
            : Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Mozesz edytowac ewidencje czasu tylko wlasna i swojego zespolu.");
    }
}

public sealed record ClockInRequest(Guid EmployeeId, string? Note = null);
public sealed record ClockOutRequest(Guid EmployeeId, string? Note = null);
public sealed record StartBreakRequest(Guid EmployeeId, string BreakType, string? Note = null);
public sealed record EndBreakRequest(Guid EmployeeId, string? Note = null);
public sealed record TimeSheetRequest(DateOnly? From, DateOnly? To, string? Period);
public sealed record AdminCreateTimeEntryRequest(Guid EmployeeId, DateTime EntryTime, string Type, string? BreakType = null, string? Note = null);
public sealed record AdminUpdateTimeEntryRequest(DateTime EntryTime, string Type, string? BreakType = null, string? Note = null);
