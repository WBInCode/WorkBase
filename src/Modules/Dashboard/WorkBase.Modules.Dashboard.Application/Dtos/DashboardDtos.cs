namespace WorkBase.Modules.Dashboard.Application.Dtos;

public sealed record DashboardSummaryDto(
    AttendanceSummaryDto Attendance,
    TaskSummaryDto Tasks,
    LeaveSummaryDto Leave,
    AnomalySummaryDto Anomalies);

public sealed record AttendanceSummaryDto(
    int PresentToday,
    int LateToday,
    int AbsentToday,
    int TotalScheduled);

public sealed record TaskSummaryDto(
    int OpenTasks,
    int OverdueTasks,
    int CompletedThisWeek,
    int TotalTasks);

public sealed record LeaveSummaryDto(
    int PendingRequests,
    int ApprovedThisMonth,
    int OnLeaveToday);

public sealed record AnomalySummaryDto(
    int NewAnomalies,
    int ReviewedThisWeek);
