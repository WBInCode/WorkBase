using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Dashboard.Application.Contracts;
using WorkBase.Modules.Dashboard.Domain.Entities;

namespace WorkBase.Modules.Dashboard.Infrastructure.Repositories;

public sealed class DashboardConfigRepository(WorkBaseDbContext db) : IDashboardConfigRepository
{
    public async Task<DashboardConfig?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<DashboardConfig>().FindAsync([id], ct);

    public async Task<List<DashboardConfig>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await db.Set<DashboardConfig>().Where(c => c.UserId == userId).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<DashboardConfig?> GetDefaultAsync(Guid userId, CancellationToken ct = default)
        => await db.Set<DashboardConfig>().FirstOrDefaultAsync(c => c.UserId == userId && c.IsDefault, ct);

    public async Task AddAsync(DashboardConfig config, CancellationToken ct = default)
        => await db.Set<DashboardConfig>().AddAsync(config, ct);

    public void Update(DashboardConfig config) => db.Set<DashboardConfig>().Update(config);
    public void Remove(DashboardConfig config) => db.Set<DashboardConfig>().Remove(config);
}

public sealed class DashboardWidgetRepository(WorkBaseDbContext db) : IDashboardWidgetRepository
{
    public async Task<List<DashboardWidget>> GetByConfigAsync(Guid configId, CancellationToken ct = default)
        => await db.Set<DashboardWidget>().Where(w => w.DashboardConfigId == configId).OrderBy(w => w.SortOrder).ToListAsync(ct);

    public async Task AddAsync(DashboardWidget widget, CancellationToken ct = default)
        => await db.Set<DashboardWidget>().AddAsync(widget, ct);

    public async Task AddRangeAsync(IEnumerable<DashboardWidget> widgets, CancellationToken ct = default)
        => await db.Set<DashboardWidget>().AddRangeAsync(widgets, ct);

    public void Update(DashboardWidget widget) => db.Set<DashboardWidget>().Update(widget);
    public void Remove(DashboardWidget widget) => db.Set<DashboardWidget>().Remove(widget);
    public void RemoveRange(IEnumerable<DashboardWidget> widgets) => db.Set<DashboardWidget>().RemoveRange(widgets);
}

public sealed class ReportDefinitionRepository(WorkBaseDbContext db) : IReportDefinitionRepository
{
    public async Task<ReportDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<ReportDefinition>().FindAsync([id], ct);

    public async Task<List<ReportDefinition>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await db.Set<ReportDefinition>().Where(r => r.TenantId == tenantId).OrderBy(r => r.Name).ToListAsync(ct);

    public async Task<List<ReportDefinition>> GetSharedAsync(Guid tenantId, CancellationToken ct = default)
        => await db.Set<ReportDefinition>().Where(r => r.TenantId == tenantId && r.IsShared).ToListAsync(ct);

    public async Task AddAsync(ReportDefinition report, CancellationToken ct = default)
        => await db.Set<ReportDefinition>().AddAsync(report, ct);

    public void Update(ReportDefinition report) => db.Set<ReportDefinition>().Update(report);
    public void Remove(ReportDefinition report) => db.Set<ReportDefinition>().Remove(report);
}
