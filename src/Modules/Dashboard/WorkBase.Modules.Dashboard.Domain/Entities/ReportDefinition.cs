using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Dashboard.Domain.Entities;

public sealed class ReportDefinition : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string ReportType { get; private set; } = null!; // table, chart, summary, pivot
    public string DataSource { get; private set; } = null!; // employees, time_entries, leave_requests, tasks, cases, contacts, form_submissions
    public string? FiltersJson { get; private set; } // JSON: { "status": "active", "dateRange": "last30days" }
    public string? ColumnsJson { get; private set; } // JSON: ["firstName","lastName","department"]
    public string? GroupByJson { get; private set; } // JSON: ["department","status"]
    public string? AggregationsJson { get; private set; } // JSON: [{"field":"hours","fn":"sum"},{"field":"id","fn":"count"}]
    public string? ChartConfigJson { get; private set; } // JSON: { "type":"bar","xAxis":"department","yAxis":"count" }
    public string? SortJson { get; private set; } // JSON: [{"field":"createdAt","dir":"desc"}]
    public bool IsShared { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private ReportDefinition() { }

    public static ReportDefinition Create(
        Guid tenantId, string name, string reportType, string dataSource,
        Guid createdByUserId, string? description = null, bool isShared = false)
    {
        return new ReportDefinition
        {
            TenantId = tenantId,
            Name = name,
            ReportType = reportType,
            DataSource = dataSource,
            CreatedByUserId = createdByUserId,
            Description = description,
            IsShared = isShared,
        };
    }

    public void Update(string name, string? description, string reportType, string dataSource,
        string? filtersJson, string? columnsJson, string? groupByJson,
        string? aggregationsJson, string? chartConfigJson, string? sortJson, bool isShared)
    {
        Name = name;
        Description = description;
        ReportType = reportType;
        DataSource = dataSource;
        FiltersJson = filtersJson;
        ColumnsJson = columnsJson;
        GroupByJson = groupByJson;
        AggregationsJson = aggregationsJson;
        ChartConfigJson = chartConfigJson;
        SortJson = sortJson;
        IsShared = isShared;
    }
}
