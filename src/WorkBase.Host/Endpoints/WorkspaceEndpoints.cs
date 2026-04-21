using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.TimeTracking.Application.Queries;
using WorkBase.Modules.Tasks.Application.Queries;
using WorkBase.Modules.Workflow.Application.Queries;
using WorkBase.Modules.Leave.Application.Queries;
using WorkBase.Shared.Api;

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

    private static async Task<IResult> GetMyDay(Guid employeeId, ISender sender, int? year = null)
    {
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
