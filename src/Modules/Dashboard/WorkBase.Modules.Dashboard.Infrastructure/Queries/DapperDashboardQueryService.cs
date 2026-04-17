using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using WorkBase.Modules.Dashboard.Application.Contracts;
using WorkBase.Modules.Dashboard.Application.Dtos;

namespace WorkBase.Modules.Dashboard.Infrastructure.Queries;

public sealed class DapperDashboardQueryService(IConfiguration configuration) : IDashboardQueryService
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    public async Task<DashboardSummaryDto> GetSummaryAsync(
        Guid tenantId, IReadOnlyList<Guid>? visibleUnitIds, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var unitFilter = BuildUnitFilter(visibleUnitIds);

        var attendance = await GetAttendanceAsync(connection, tenantId, unitFilter);
        var tasks = await GetTaskSummaryAsync(connection, tenantId, unitFilter);
        var leave = await GetLeaveSummaryAsync(connection, tenantId, unitFilter);
        var anomalies = await GetAnomalySummaryAsync(connection, tenantId, unitFilter);

        return new DashboardSummaryDto(attendance, tasks, leave, anomalies);
    }

    private static async Task<AttendanceSummaryDto> GetAttendanceAsync(
        NpgsqlConnection connection, Guid tenantId, UnitFilter unitFilter)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        const string sql = """
            WITH scheduled AS (
                SELECT DISTINCT s.employee_id
                FROM time_schedules s
                INNER JOIN org_employees e ON e.id = s.employee_id AND e.tenant_id = s.tenant_id
                WHERE s.tenant_id = @TenantId
                  AND s.date = @Today::date
                  {0}
            ),
            clock_ins AS (
                SELECT DISTINCT te.employee_id,
                       MIN(te.entry_time) AS first_clock_in
                FROM time_entries te
                INNER JOIN org_employees e ON e.id = te.employee_id AND e.tenant_id = te.tenant_id
                WHERE te.tenant_id = @TenantId
                  AND te.type = 0
                  AND te.entry_time >= @Today AND te.entry_time < @Tomorrow
                  {0}
                GROUP BY te.employee_id
            )
            SELECT
                (SELECT COUNT(*) FROM clock_ins) AS present_today,
                (SELECT COUNT(*) FROM clock_ins ci
                    INNER JOIN time_schedules s ON s.employee_id = ci.employee_id
                        AND s.tenant_id = @TenantId AND s.date = @Today::date
                    WHERE ci.first_clock_in > (s.date + s.planned_start)::timestamp + INTERVAL '15 minutes'
                ) AS late_today,
                (SELECT COUNT(*) FROM scheduled sc
                    WHERE sc.employee_id NOT IN (SELECT employee_id FROM clock_ins)
                ) AS absent_today,
                (SELECT COUNT(*) FROM scheduled) AS total_scheduled
            """;

        var formatted = string.Format(sql, unitFilter.EmployeeJoinClause("e"));
        var row = await connection.QuerySingleAsync<AttendanceRow>(formatted, new
        {
            TenantId = tenantId,
            Today = today,
            Tomorrow = tomorrow,
            unitFilter.UnitIds,
        });

        return new AttendanceSummaryDto(row.PresentToday, row.LateToday, row.AbsentToday, row.TotalScheduled);
    }

    private static async Task<TaskSummaryDto> GetTaskSummaryAsync(
        NpgsqlConnection connection, Guid tenantId, UnitFilter unitFilter)
    {
        var now = DateTime.UtcNow;
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
        if (now.DayOfWeek == DayOfWeek.Sunday) weekStart = weekStart.AddDays(-7);

        const string sql = """
            SELECT
                COUNT(*) FILTER (WHERE t.completed_at IS NULL AND NOT ts.is_final) AS open_tasks,
                COUNT(*) FILTER (WHERE t.completed_at IS NULL AND NOT ts.is_final
                    AND t.due_date IS NOT NULL AND t.due_date < @Now) AS overdue_tasks,
                COUNT(*) FILTER (WHERE t.completed_at IS NOT NULL
                    AND t.completed_at >= @WeekStart) AS completed_this_week,
                COUNT(*) AS total_tasks
            FROM task_tasks t
            INNER JOIN task_statuses ts ON ts.id = t.status_id
            {0}
            WHERE t.tenant_id = @TenantId
            """;

        var employeeJoin = unitFilter.HasFilter
            ? "INNER JOIN org_employees e ON e.id = t.assignee_id AND e.tenant_id = t.tenant_id " +
              unitFilter.EmployeeWhereClause("e")
            : "";

        var formatted = string.Format(sql, employeeJoin);
        var row = await connection.QuerySingleAsync<TaskRow>(formatted, new
        {
            TenantId = tenantId,
            Now = now,
            WeekStart = weekStart,
            unitFilter.UnitIds,
        });

        return new TaskSummaryDto(row.OpenTasks, row.OverdueTasks, row.CompletedThisWeek, row.TotalTasks);
    }

    private static async Task<LeaveSummaryDto> GetLeaveSummaryAsync(
        NpgsqlConnection connection, Guid tenantId, UnitFilter unitFilter)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        const string sql = """
            SELECT
                COUNT(*) FILTER (WHERE lr.status = 1) AS pending_requests,
                COUNT(*) FILTER (WHERE lr.status = 2
                    AND lr.modified_at >= @MonthStart) AS approved_this_month,
                COUNT(*) FILTER (WHERE lr.status = 2
                    AND lr.start_date <= @Today::date AND lr.end_date >= @Today::date) AS on_leave_today
            FROM leave_requests lr
            {0}
            WHERE lr.tenant_id = @TenantId
            """;

        var employeeJoin = unitFilter.HasFilter
            ? "INNER JOIN org_employees e ON e.id = lr.employee_id AND e.tenant_id = lr.tenant_id " +
              unitFilter.EmployeeWhereClause("e")
            : "";

        var formatted = string.Format(sql, employeeJoin);
        var row = await connection.QuerySingleAsync<LeaveRow>(formatted, new
        {
            TenantId = tenantId,
            Today = today,
            MonthStart = monthStart,
            unitFilter.UnitIds,
        });

        return new LeaveSummaryDto(row.PendingRequests, row.ApprovedThisMonth, row.OnLeaveToday);
    }

    private static async Task<AnomalySummaryDto> GetAnomalySummaryAsync(
        NpgsqlConnection connection, Guid tenantId, UnitFilter unitFilter)
    {
        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + (int)DayOfWeek.Monday);
        if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday) weekStart = weekStart.AddDays(-7);

        const string sql = """
            SELECT
                COUNT(*) FILTER (WHERE a.status = 0) AS new_anomalies,
                COUNT(*) FILTER (WHERE a.status = 1
                    AND a.reviewed_at >= @WeekStart) AS reviewed_this_week
            FROM time_anomalies a
            {0}
            WHERE a.tenant_id = @TenantId
            """;

        var employeeJoin = unitFilter.HasFilter
            ? "INNER JOIN org_employees e ON e.id = a.employee_id AND e.tenant_id = a.tenant_id " +
              unitFilter.EmployeeWhereClause("e")
            : "";

        var formatted = string.Format(sql, employeeJoin);
        var row = await connection.QuerySingleAsync<AnomalyRow>(formatted, new
        {
            TenantId = tenantId,
            WeekStart = weekStart,
            unitFilter.UnitIds,
        });

        return new AnomalySummaryDto(row.NewAnomalies, row.ReviewedThisWeek);
    }

    private static UnitFilter BuildUnitFilter(IReadOnlyList<Guid>? visibleUnitIds)
        => new(visibleUnitIds is { Count: > 0 } ? visibleUnitIds : null);

    private sealed class UnitFilter(IReadOnlyList<Guid>? unitIds)
    {
        public IReadOnlyList<Guid>? UnitIds => unitIds;
        public bool HasFilter => unitIds is { Count: > 0 };

        public string EmployeeJoinClause(string alias)
            => HasFilter
                ? $"AND {alias}.organization_unit_id = ANY(@UnitIds)"
                : "";

        public string EmployeeWhereClause(string alias)
            => HasFilter
                ? $"AND {alias}.organization_unit_id = ANY(@UnitIds)"
                : "";
    }

    // Dapper row mappings (snake_case → PascalCase via Dapper default matching)
    private sealed record AttendanceRow
    {
        public int PresentToday { get; init; }
        public int LateToday { get; init; }
        public int AbsentToday { get; init; }
        public int TotalScheduled { get; init; }
    }

    private sealed record TaskRow
    {
        public int OpenTasks { get; init; }
        public int OverdueTasks { get; init; }
        public int CompletedThisWeek { get; init; }
        public int TotalTasks { get; init; }
    }

    private sealed record LeaveRow
    {
        public int PendingRequests { get; init; }
        public int ApprovedThisMonth { get; init; }
        public int OnLeaveToday { get; init; }
    }

    private sealed record AnomalyRow
    {
        public int NewAnomalies { get; init; }
        public int ReviewedThisWeek { get; init; }
    }
}
