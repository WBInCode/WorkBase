using WorkBase.Modules.Dashboard.Domain.Entities;

namespace WorkBase.Modules.Dashboard.Application.Contracts;

public interface IDashboardConfigRepository
{
    Task<DashboardConfig?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<DashboardConfig>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<DashboardConfig?> GetDefaultAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(DashboardConfig config, CancellationToken ct = default);
    void Update(DashboardConfig config);
    void Remove(DashboardConfig config);
}

public interface IDashboardWidgetRepository
{
    Task<List<DashboardWidget>> GetByConfigAsync(Guid configId, CancellationToken ct = default);
    Task AddAsync(DashboardWidget widget, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<DashboardWidget> widgets, CancellationToken ct = default);
    void Update(DashboardWidget widget);
    void Remove(DashboardWidget widget);
    void RemoveRange(IEnumerable<DashboardWidget> widgets);
}

public interface IReportDefinitionRepository
{
    Task<ReportDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ReportDefinition>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<List<ReportDefinition>> GetSharedAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(ReportDefinition report, CancellationToken ct = default);
    void Update(ReportDefinition report);
    void Remove(ReportDefinition report);
}
