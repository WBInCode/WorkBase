using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Application.Contracts;
using WorkBase.Modules.Identity.Domain.Entities;

namespace WorkBase.Modules.Identity.Infrastructure.Services;

public sealed class FeatureFlagService(WorkBaseDbContext db) : IFeatureFlagService
{
    public async Task<List<FeatureFlag>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await db.Set<FeatureFlag>().Where(f => f.TenantId == tenantId).ToListAsync(ct);

    public async Task ToggleAsync(Guid tenantId, string module, string? userId, CancellationToken ct = default)
    {
        var flag = await db.Set<FeatureFlag>()
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.Module == module, ct);

        if (flag is null)
        {
            flag = FeatureFlag.Create(tenantId, module, true, userId);
            await db.Set<FeatureFlag>().AddAsync(flag, ct);
        }
        else if (flag.IsEnabled)
        {
            flag.Disable();
        }
        else
        {
            flag.Enable(userId);
        }
    }
}
