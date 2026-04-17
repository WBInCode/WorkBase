using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Dashboard.Application.Contracts;
using WorkBase.Modules.Dashboard.Application.Dtos;
using WorkBase.Modules.Dashboard.Application.Queries;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Dashboard.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/summary", GetSummary)
            .WithName("GetDashboardSummary")
            .WithSummary("Pobierz podsumowanie dashboardu")
            .RequirePermission("dashboard.view")
            .Produces<DashboardSummaryDto>();

        group.MapGet("/attendance-today", GetAttendanceToday)
            .WithName("GetAttendanceToday")
            .WithSummary("Obecność dzisiaj")
            .RequirePermission("dashboard.view")
            .Produces<AttendanceSummaryDto>();

        group.MapGet("/late-arrivals", GetLateArrivals)
            .WithName("GetLateArrivals")
            .WithSummary("Spóźnienia dzisiaj")
            .RequirePermission("dashboard.view")
            .Produces<int>();

        group.MapGet("/open-tasks", GetOpenTasks)
            .WithName("GetOpenTasksCount")
            .WithSummary("Otwarte zadania")
            .RequirePermission("dashboard.view")
            .Produces<TaskSummaryDto>();

        group.MapGet("/pending-approvals", GetPendingApprovals)
            .WithName("GetPendingApprovalsCount")
            .WithSummary("Oczekujące zatwierdzenia")
            .RequirePermission("dashboard.view")
            .Produces<int>();

        group.MapGet("/anomalies", GetAnomalies)
            .WithName("GetAnomaliesCount")
            .WithSummary("Anomalie")
            .RequirePermission("dashboard.view")
            .Produces<AnomalySummaryDto>();

        group.MapGet("/alerts", GetAlerts)
            .WithName("GetDashboardAlerts")
            .WithSummary("Alerty dashboardu")
            .RequirePermission("dashboard.view")
            .Produces<List<DashboardAlertDto>>();

        group.MapGet("/reports/export", ExportReport)
            .WithName("ExportDashboardReport")
            .WithSummary("Eksportuj raport CSV")
            .RequirePermission("dashboard.view")
            .Produces(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<IResult> GetSummary(ISender sender)
    {
        var result = await sender.Send(new GetDashboardSummaryQuery());
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAttendanceToday(ISender sender)
    {
        var result = await sender.Send(new GetDashboardSummaryQuery());
        return result.IsSuccess ? Results.Ok(result.Value.Attendance) : result.ToHttpResult();
    }

    private static async Task<IResult> GetLateArrivals(ISender sender)
    {
        var result = await sender.Send(new GetDashboardSummaryQuery());
        return result.IsSuccess ? Results.Ok(result.Value.Attendance.LateToday) : result.ToHttpResult();
    }

    private static async Task<IResult> GetOpenTasks(ISender sender)
    {
        var result = await sender.Send(new GetDashboardSummaryQuery());
        return result.IsSuccess ? Results.Ok(result.Value.Tasks) : result.ToHttpResult();
    }

    private static async Task<IResult> GetPendingApprovals(ISender sender)
    {
        var result = await sender.Send(new GetDashboardSummaryQuery());
        return result.IsSuccess ? Results.Ok(result.Value.Leave.PendingRequests) : result.ToHttpResult();
    }

    private static async Task<IResult> GetAnomalies(ISender sender)
    {
        var result = await sender.Send(new GetDashboardSummaryQuery());
        return result.IsSuccess ? Results.Ok(result.Value.Anomalies) : result.ToHttpResult();
    }

    private static async Task<IResult> GetAlerts(ISender sender)
    {
        var result = await sender.Send(new GetDashboardSummaryQuery());
        if (!result.IsSuccess) return result.ToHttpResult();

        var alerts = new List<DashboardAlertDto>();
        var s = result.Value;

        if (s.Anomalies.NewAnomalies > 0)
            alerts.Add(new DashboardAlertDto("anomalies", "warning",
                $"{s.Anomalies.NewAnomalies} nowych anomalii wymaga przeglądu."));
        if (s.Tasks.OverdueTasks > 0)
            alerts.Add(new DashboardAlertDto("tasks", "error",
                $"{s.Tasks.OverdueTasks} zadań jest przeterminowanych."));
        if (s.Leave.PendingRequests > 0)
            alerts.Add(new DashboardAlertDto("leave", "info",
                $"{s.Leave.PendingRequests} wniosków urlopowych oczekuje na zatwierdzenie."));

        return Results.Ok(alerts);
    }

    private static async Task<IResult> ExportReport(ISender sender)
    {
        var result = await sender.Send(new GetDashboardSummaryQuery());
        if (!result.IsSuccess) return result.ToHttpResult();

        var s = result.Value;
        var csv = "Kategoria;Metryka;Wartość\n"
            + $"Obecność;Obecni;{s.Attendance.PresentToday}\n"
            + $"Obecność;Spóźnieni;{s.Attendance.LateToday}\n"
            + $"Obecność;Nieobecni;{s.Attendance.AbsentToday}\n"
            + $"Zadania;Otwarte;{s.Tasks.OpenTasks}\n"
            + $"Zadania;Przeterminowane;{s.Tasks.OverdueTasks}\n"
            + $"Zadania;Ukończone (tydzień);{s.Tasks.CompletedThisWeek}\n"
            + $"Urlopy;Oczekujące;{s.Leave.PendingRequests}\n"
            + $"Urlopy;Na urlopie dziś;{s.Leave.OnLeaveToday}\n"
            + $"Anomalie;Nowe;{s.Anomalies.NewAnomalies}\n";

        return Results.Text(csv, "text/csv");
    }
}

public sealed record DashboardAlertDto(string Category, string Severity, string Message);
