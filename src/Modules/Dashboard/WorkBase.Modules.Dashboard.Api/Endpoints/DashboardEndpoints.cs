using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Dashboard.Application.Commands;
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

        // --- Dashboard Configs ---
        group.MapGet("/configs/{userId:guid}", async (Guid userId, ISender sender) =>
        {
            var result = await sender.Send(new GetDashboardConfigsQuery(userId));
            return result.ToHttpResult();
        })
        .WithName("GetDashboardConfigs")
        .WithSummary("Pobierz konfiguracje dashboardu użytkownika")
        .RequirePermission("dashboard.view")
        .Produces<List<DashboardConfigDto>>();

        group.MapPost("/configs", async (CreateDashboardConfigBody body, ISender sender) =>
        {
            var cmd = new CreateDashboardConfigCommand(body.UserId, body.Name, body.IsDefault, body.Widgets);
            var result = await sender.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/dashboard/configs/{result.Value}", result.Value)
                : result.ToHttpResult();
        })
        .WithName("CreateDashboardConfig")
        .WithSummary("Utwórz konfigurację dashboardu")
        .RequirePermission("dashboard.manage")
        .Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/configs/{id:guid}", async (Guid id, UpdateDashboardConfigBody body, ISender sender) =>
        {
            var cmd = new UpdateDashboardConfigCommand(id, body.Name, body.IsDefault, body.Widgets);
            var result = await sender.Send(cmd);
            return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
        })
        .WithName("UpdateDashboardConfig")
        .WithSummary("Aktualizuj konfigurację dashboardu")
        .RequirePermission("dashboard.manage")
        .Produces(StatusCodes.Status204NoContent);

        group.MapDelete("/configs/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteDashboardConfigCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
        })
        .WithName("DeleteDashboardConfig")
        .WithSummary("Usuń konfigurację dashboardu")
        .RequirePermission("dashboard.manage")
        .Produces(StatusCodes.Status204NoContent);

        // --- Reports ---
        group.MapGet("/reports", async (ISender sender) =>
        {
            var result = await sender.Send(new GetReportsQuery());
            return result.ToHttpResult();
        })
        .WithName("GetReports")
        .WithSummary("Pobierz listę raportów")
        .RequirePermission("reports.view")
        .Produces<List<ReportDefinitionDto>>();

        group.MapGet("/reports/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetReportByIdQuery(id));
            return result.ToHttpResult();
        })
        .WithName("GetReportById")
        .WithSummary("Pobierz raport po ID")
        .RequirePermission("reports.view")
        .Produces<ReportDefinitionDto>();

        group.MapPost("/reports", async (CreateReportBody body, ISender sender) =>
        {
            var cmd = new CreateReportCommand(body.Name, body.Description, body.ReportType,
                body.DataSource, body.FiltersJson, body.ColumnsJson, body.GroupByJson,
                body.AggregationsJson, body.ChartConfigJson, body.SortJson,
                body.IsShared, body.CreatedByUserId);
            var result = await sender.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/dashboard/reports/{result.Value}", result.Value)
                : result.ToHttpResult();
        })
        .WithName("CreateReport")
        .WithSummary("Utwórz nowy raport")
        .RequirePermission("reports.manage")
        .Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/reports/{id:guid}", async (Guid id, UpdateReportBody body, ISender sender) =>
        {
            var cmd = new UpdateReportCommand(id, body.Name, body.Description, body.ReportType,
                body.DataSource, body.FiltersJson, body.ColumnsJson, body.GroupByJson,
                body.AggregationsJson, body.ChartConfigJson, body.SortJson, body.IsShared);
            var result = await sender.Send(cmd);
            return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
        })
        .WithName("UpdateReport")
        .WithSummary("Aktualizuj raport")
        .RequirePermission("reports.manage")
        .Produces(StatusCodes.Status204NoContent);

        group.MapDelete("/reports/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteReportCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
        })
        .WithName("DeleteReport")
        .WithSummary("Usuń raport")
        .RequirePermission("reports.manage")
        .Produces(StatusCodes.Status204NoContent);

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

public sealed record CreateDashboardConfigBody(
    Guid UserId, string Name, bool IsDefault, List<CreateWidgetRequest> Widgets);

public sealed record UpdateDashboardConfigBody(
    string Name, bool IsDefault, List<CreateWidgetRequest> Widgets);

public sealed record CreateReportBody(
    string Name, string? Description, string ReportType, string DataSource,
    string? FiltersJson, string? ColumnsJson, string? GroupByJson,
    string? AggregationsJson, string? ChartConfigJson, string? SortJson,
    bool IsShared, Guid CreatedByUserId);

public sealed record UpdateReportBody(
    string Name, string? Description, string ReportType, string DataSource,
    string? FiltersJson, string? ColumnsJson, string? GroupByJson,
    string? AggregationsJson, string? ChartConfigJson, string? SortJson,
    bool IsShared);
