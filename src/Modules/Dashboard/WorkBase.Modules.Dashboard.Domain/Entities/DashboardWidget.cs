using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Dashboard.Domain.Entities;

public sealed class DashboardWidget : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid DashboardConfigId { get; private set; }
    public string WidgetType { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public int Column { get; private set; }
    public int Row { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string? Settings { get; private set; }
    public bool IsVisible { get; private set; }
    public int SortOrder { get; private set; }

    private DashboardWidget() { }

    public static DashboardWidget Create(
        Guid tenantId, Guid dashboardConfigId,
        string widgetType, string title,
        int column, int row, int width, int height,
        string? settings = null, int sortOrder = 0)
    {
        return new DashboardWidget
        {
            TenantId = tenantId,
            DashboardConfigId = dashboardConfigId,
            WidgetType = widgetType,
            Title = title,
            Column = column,
            Row = row,
            Width = width,
            Height = height,
            Settings = settings,
            IsVisible = true,
            SortOrder = sortOrder,
        };
    }

    public void UpdateLayout(int column, int row, int width, int height)
    {
        Column = column;
        Row = row;
        Width = width;
        Height = height;
    }

    public void UpdateSettings(string? settings)
    {
        Settings = settings;
    }

    public void SetVisibility(bool isVisible)
    {
        IsVisible = isVisible;
    }
}
