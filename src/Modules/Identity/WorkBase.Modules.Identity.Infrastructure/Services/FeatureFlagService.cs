using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Persistence.Entities;
using WorkBase.Modules.Identity.Application.Contracts;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Shared.Domain;
using WorkBase.Shared.Modules;

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

        // Called directly from a minimal API endpoint (not through MediatR), so there is no
        // UnitOfWorkBehavior to save changes automatically — must do it explicitly here.
        await db.SaveChangesAsync(ct);
    }

    public async Task<Result> ApplyPlanAsync(Guid tenantId, Guid planId, string? userId, CancellationToken ct = default)
    {
        var plan = await db.Set<LicensePlan>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId && p.IsActive, ct);

        if (plan is null)
            return Result.Failure(new Error("LicensePlan.NotFound", "Nie znaleziono aktywnego planu licencyjnego.", ErrorType.NotFound));

        var existingFlags = await db.Set<FeatureFlag>()
            .Where(f => f.TenantId == tenantId)
            .ToListAsync(ct);

        foreach (var module in ModuleCatalog.All)
        {
            var shouldBeEnabled = plan.IncludedModules.Contains(module.Key);
            var flag = existingFlags.FirstOrDefault(f => f.Module == module.Key);

            if (flag is null)
            {
                flag = FeatureFlag.Create(tenantId, module.Key, shouldBeEnabled, userId);
                await db.Set<FeatureFlag>().AddAsync(flag, ct);
            }
            else if (shouldBeEnabled && !flag.IsEnabled)
            {
                flag.Enable(userId);
            }
            else if (!shouldBeEnabled && flag.IsEnabled)
            {
                flag.Disable();
            }
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
