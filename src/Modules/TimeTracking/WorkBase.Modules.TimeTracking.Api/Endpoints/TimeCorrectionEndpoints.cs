using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using WorkBase.Contracts;
using WorkBase.Modules.TimeTracking.Application.Commands;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Queries;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.TimeTracking.Api.Endpoints;

public static class TimeCorrectionEndpoints
{
    public static IEndpointRouteBuilder MapTimeCorrectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/time/corrections")
            .WithTags("TimeTracking – Corrections")
            .RequireAuthorization();

        group.MapGet("/{employeeId:guid}", GetCorrections)
            .WithName("GetTimeCorrections")
            .WithSummary("Pobierz korekty czasu pracy pracownika")
            .RequirePermission("time.view")
            .Produces<List<TimeCorrectionDto>>();

        group.MapPost("/", CreateCorrection)
            .WithName("CreateTimeCorrection")
            .WithSummary("Utwórz korektę czasu pracy")
            .RequireAnyPermission("time.manage", "time.edit")
            .Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", UpdateCorrection)
            .WithName("UpdateTimeCorrection")
            .WithSummary("Zaktualizuj korektę czasu pracy")
            .RequireAnyPermission("time.manage", "time.edit")
            .Produces(StatusCodes.Status200OK);

        group.MapDelete("/{id:guid}", DeleteCorrection)
            .WithName("DeleteTimeCorrection")
            .WithSummary("Usuń korektę czasu pracy")
            .RequireAnyPermission("time.manage", "time.edit")
            .Produces(StatusCodes.Status204NoContent);

        return endpoints;
    }

    private static async Task<IResult> GetCorrections(
        Guid employeeId,
        DateOnly? from,
        DateOnly? to,
        ISender sender)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await sender.Send(new GetTimeCorrectionsQuery(employeeId, fromDate, toDate));
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateCorrection(
        CreateTimeCorrectionRequest request,
        ClaimsPrincipal caller,
        ITimeManagementScopeService scope,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureCanManage(caller, scope, request.EmployeeId, cancellationToken);
        if (denied is not null)
            return denied;

        var command = new CreateTimeCorrectionCommand(
            request.EmployeeId,
            request.Date,
            request.OriginalClockIn,
            request.OriginalClockOut,
            request.CorrectedClockIn,
            request.CorrectedClockOut,
            request.Reason,
            request.CorrectedBy,
            request.TimeSheetId);

        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/time/corrections/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdateCorrection(
        Guid id,
        UpdateTimeCorrectionRequest request,
        ClaimsPrincipal caller,
        ITimeManagementScopeService scope,
        ITimeCorrectionRepository corrections,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureCanManageCorrection(caller, scope, corrections, id, cancellationToken);
        if (denied is not null)
            return denied;

        var command = new UpdateTimeCorrectionCommand(
            id, request.CorrectedClockIn, request.CorrectedClockOut, request.Reason);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteCorrection(
        Guid id,
        ClaimsPrincipal caller,
        ITimeManagementScopeService scope,
        ITimeCorrectionRepository corrections,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureCanManageCorrection(caller, scope, corrections, id, cancellationToken);
        if (denied is not null)
            return denied;

        var result = await sender.Send(new DeleteTimeCorrectionCommand(id), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult?> EnsureCanManageCorrection(
        ClaimsPrincipal caller,
        ITimeManagementScopeService scope,
        ITimeCorrectionRepository corrections,
        Guid correctionId,
        CancellationToken cancellationToken)
    {
        var tenantId = PermissionClaims.GetTenantId(caller);
        if (tenantId is null)
            return Results.Forbid();

        var correction = await corrections.GetByIdAsync(tenantId.Value, correctionId, cancellationToken);
        if (correction is null)
            return Results.NotFound();

        return await EnsureCanManage(caller, scope, correction.EmployeeId, cancellationToken);
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

public sealed record CreateTimeCorrectionRequest(
    Guid EmployeeId,
    DateOnly Date,
    DateTime OriginalClockIn,
    DateTime OriginalClockOut,
    DateTime CorrectedClockIn,
    DateTime CorrectedClockOut,
    string Reason,
    string CorrectedBy,
    Guid? TimeSheetId = null);

public sealed record UpdateTimeCorrectionRequest(
    DateTime CorrectedClockIn, DateTime CorrectedClockOut, string Reason);
