using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.TimeTracking.Application.Commands;
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
            .RequirePermission("time.manage")
            .Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", UpdateCorrection)
            .WithName("UpdateTimeCorrection")
            .WithSummary("Zaktualizuj korektę czasu pracy")
            .RequirePermission("time.manage")
            .Produces(StatusCodes.Status200OK);

        group.MapDelete("/{id:guid}", DeleteCorrection)
            .WithName("DeleteTimeCorrection")
            .WithSummary("Usuń korektę czasu pracy")
            .RequirePermission("time.manage")
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
        ISender sender)
    {
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

        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Created($"/api/time/corrections/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdateCorrection(Guid id, UpdateTimeCorrectionRequest request, ISender sender)
    {
        var command = new UpdateTimeCorrectionCommand(
            id, request.CorrectedClockIn, request.CorrectedClockOut, request.Reason);
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteCorrection(Guid id, ISender sender)
    {
        var result = await sender.Send(new DeleteTimeCorrectionCommand(id));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
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
