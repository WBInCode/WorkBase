using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using WorkBase.Modules.TimeTracking.Application.Queries;
using WorkBase.Modules.Tasks.Application.Queries;
using WorkBase.Modules.Workflow.Application.Queries;
using WorkBase.Modules.Leave.Application.Queries;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Host.Endpoints;

public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/workspace")
            .WithTags("Workspace")
            .RequireAuthorization();

        group.MapGet("/my-day/{employeeId:guid}", GetMyDay)
            .WithName("GetMyDay")
            .WithSummary("Agregowany widok 'Mój dzień' dla pracownika");

        return endpoints;
    }

    private static async Task<IResult> GetMyDay(
        Guid employeeId,
        ClaimsPrincipal user,
        IPermissionService permissions,
        IEmployeeScopeResolver scopes,
        ISender sender,
        CancellationToken ct,
        int? year = null)
    {
        // Identyfikator pracownika przychodzi wprost z adresu, a endpoint zwraca wnioski
        // urlopowe razem z polem "powod", zadania i status czasu pracy. Bez tego sprawdzenia
        // wystarczylo podmienic identyfikator w adresie, zeby zobaczyc dane kolegi z firmy.
        // Ten sam warunek stoi przy /api/leave/requests/{employeeId} — tutaj go brakowalo,
        // mimo ze wolane jest dokladnie to samo zapytanie.
        if (!await user.CanAccessEmployeeAsync(employeeId, permissions, scopes, "leave.view-team", "leave", ct))
            return Results.Forbid();

        var timeTask = sender.Send(new GetCurrentStatusQuery(employeeId));
        var tasksTask = sender.Send(new GetTasksQuery(employeeId));
        var approvalsTask = sender.Send(new GetPendingApprovalsQuery(employeeId));
        var leaveTask = sender.Send(new GetLeaveRequestsQuery(employeeId, year));

        await Task.WhenAll(timeTask, tasksTask, approvalsTask, leaveTask);

        var timeResult = await timeTask;
        var tasksResult = await tasksTask;
        var approvalsResult = await approvalsTask;
        var leaveResult = await leaveTask;

        return Results.Ok(new
        {
            TimeStatus = timeResult.IsSuccess ? timeResult.Value : null,
            Tasks = tasksResult.IsSuccess ? tasksResult.Value : [],
            PendingApprovals = approvalsResult.IsSuccess ? approvalsResult.Value : [],
            LeaveRequests = leaveResult.IsSuccess ? leaveResult.Value : []
        });
    }
}
