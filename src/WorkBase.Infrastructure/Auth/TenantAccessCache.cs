using Microsoft.Extensions.Caching.Memory;

namespace WorkBase.Infrastructure.Auth;

public sealed class TenantAccessCache(IMemoryCache cache)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(15);
    private static string Key(Guid tenantId) => $"tenant-access:{tenantId}";

    public bool TryGet(Guid tenantId, out TenantAccessState? state) =>
        cache.TryGetValue(Key(tenantId), out state);

    public void Set(Guid tenantId, TenantAccessState state) =>
        cache.Set(Key(tenantId), state, Lifetime);

    public void Invalidate(Guid tenantId) => cache.Remove(Key(tenantId));
}

public sealed record TenantAccessState(bool AccessAllowed, string? HubProductInstanceId);