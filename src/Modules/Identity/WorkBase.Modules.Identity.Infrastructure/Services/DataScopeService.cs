using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Application.Contracts;
using WorkBase.Modules.Identity.Domain.Entities;

namespace WorkBase.Modules.Identity.Infrastructure.Services;

public sealed class DataScopeManagementService(WorkBaseDbContext db) : IDataScopeManagementService
{
    public async Task<List<DataScope>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await db.Set<DataScope>().Where(s => s.TenantId == tenantId).ToListAsync(ct);

    public async Task<Guid> CreateAsync(Guid tenantId, Guid roleId, string module,
        DataScopeLevel scopeLevel, string? customFilter, CancellationToken ct = default)
    {
        var scope = DataScope.Create(tenantId, roleId, module, scopeLevel, customFilter);
        await db.Set<DataScope>().AddAsync(scope, ct);
        return scope.Id;
    }

    public async Task<bool> UpdateAsync(Guid id, DataScopeLevel scopeLevel,
        string? customFilter, CancellationToken ct = default)
    {
        var scope = await db.Set<DataScope>().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (scope is null) return false;
        scope.Update(scopeLevel, customFilter);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var scope = await db.Set<DataScope>().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (scope is null) return false;
        db.Set<DataScope>().Remove(scope);
        return true;
    }
}
