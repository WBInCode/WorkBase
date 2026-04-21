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

// --- Dashboard Config DTOs ---
public sealed record DashboardConfigDto(
    Guid Id, Guid UserId, string Name, bool IsDefault,
    DateTime CreatedAt, DateTime? ModifiedAt,
    List<DashboardWidgetDto> Widgets);

public sealed record DashboardWidgetDto(
    Guid Id, string WidgetType, string Title,
    int Column, int Row, int Width, int Height,
    string? Settings, bool IsVisible, int SortOrder);

// --- Report DTOs ---
public sealed record ReportDefinitionDto(
    Guid Id, string Name, string? Description,
    string ReportType, string DataSource,
    string? FiltersJson, string? ColumnsJson,
    string? GroupByJson, string? AggregationsJson,
    string? ChartConfigJson, string? SortJson,
    bool IsShared, Guid CreatedByUserId, DateTime CreatedAt);

public sealed record ReportResultDto(
    string ReportName, string ReportType,
    List<string> Columns,
    List<Dictionary<string, object?>> Rows,
    Dictionary<string, object?>? Aggregations);
