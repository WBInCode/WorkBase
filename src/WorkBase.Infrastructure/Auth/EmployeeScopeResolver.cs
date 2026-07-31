using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Auth;

namespace WorkBase.Infrastructure.Auth;

/// <summary>
/// „Zespół” to podwładni z relacji przełożonego — ta sama struktura, z której workflow wybiera
/// akceptanta wniosku. Zakres Organization/Branch znosi ograniczenie, Own zostawia tylko siebie.
/// </summary>
public sealed class EmployeeScopeResolver(WorkBaseDbContext dbContext, IMemoryCache cache) : IEmployeeScopeResolver
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<bool> CanAccessEmployeeAsync(
        Guid userId, Guid tenantId, Guid? callerEmployeeId, Guid targetEmployeeId, string module, CancellationToken ct = default)
    {
        var accessible = await FilterAccessibleAsync(userId, tenantId, callerEmployeeId, [targetEmployeeId], module, ct);
        return accessible.Contains(targetEmployeeId);
    }

    public async Task<IReadOnlySet<Guid>> FilterAccessibleAsync(
        Guid userId, Guid tenantId, Guid? callerEmployeeId, IReadOnlyCollection<Guid> targetEmployeeIds, string module, CancellationToken ct = default)
    {
        if (targetEmployeeIds.Count == 0) return new HashSet<Guid>();

        var level = await GetScopeLevelAsync(userId, tenantId, module, ct);
        if (level is DataScopeLevelValue.Branch or DataScopeLevelValue.Organization)
            return targetEmployeeIds.ToHashSet();

        var accessible = new HashSet<Guid>();
        if (callerEmployeeId is not Guid employeeId) return accessible;

        if (targetEmployeeIds.Contains(employeeId)) accessible.Add(employeeId);
        if (level == DataScopeLevelValue.Own) return accessible;

        var now = DateTime.UtcNow;
        var subordinates = await dbContext.Set<SupervisorRelation>()
            .Where(relation => relation.TenantId == tenantId
                && relation.SupervisorEmployeeId == employeeId
                && targetEmployeeIds.Contains(relation.SubordinateEmployeeId)
                && relation.StartDate <= now
                && (relation.EndDate == null || relation.EndDate > now))
            .Select(relation => relation.SubordinateEmployeeId)
            .ToListAsync(ct);

        accessible.UnionWith(subordinates);
        return accessible;
    }

    private async Task<DataScopeLevelValue> GetScopeLevelAsync(Guid userId, Guid tenantId, string module, CancellationToken ct)
    {
        var key = $"employeescope:{tenantId}:{userId}:{module}";
        if (cache.TryGetValue<DataScopeLevelValue>(key, out var cached)) return cached;

        var level = await LoadScopeLevelAsync(userId, tenantId, module, ct);
        cache.Set(key, level, CacheDuration);
        return level;
    }

    private async Task<DataScopeLevelValue> LoadScopeLevelAsync(Guid userId, Guid tenantId, string module, CancellationToken ct)
    {
        var internalUserId = await dbContext.Set<User>()
            .Where(user => user.TenantId == tenantId && (user.Id == userId || user.KeycloakId == userId.ToString()))
            .Select(user => user.Id)
            .FirstOrDefaultAsync(ct);
        if (internalUserId == Guid.Empty) return DataScopeLevelValue.Team;

        var roleIds = await dbContext.Set<UserRole>()
            .Where(userRole => userRole.UserId == internalUserId && userRole.TenantId == tenantId)
            .Select(userRole => userRole.RoleId)
            .ToListAsync(ct);
        if (roleIds.Count == 0) return DataScopeLevelValue.Team;

        // Rzutowanie na int MUSI zostac poza zapytaniem: w projekcji EF tlumaczy je na
        // scope_level::int, a kolumna trzyma nazwy poziomow i baza odrzuca zapytanie (22P02).
        var levels = await dbContext.Set<DataScope>()
            .Where(scope => scope.TenantId == tenantId && scope.Module == module && roleIds.Contains(scope.RoleId))
            .Select(scope => scope.ScopeLevel)
            .ToListAsync(ct);

        // Tenanty sprzed wprowadzenia zakresów nie mają tych wierszy — nie odbieramy wtedy dostępu,
        // który daje samo uprawnienie zespołowe.
        return levels.Count == 0 ? DataScopeLevelValue.Team : (DataScopeLevelValue)(int)levels.Max();
    }
}
