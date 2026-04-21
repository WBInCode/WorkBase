using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Integration.Application.Contracts;
using WorkBase.Modules.Integration.Domain.Entities;
using WorkBase.Modules.Integration.Domain.Enums;

namespace WorkBase.Modules.Integration.Infrastructure.Persistence;

internal sealed class IntegrationConnectionRepository(WorkBaseDbContext db) : IIntegrationConnectionRepository
{
    public async Task<IntegrationConnection?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<IntegrationConnection>().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IntegrationConnection?> GetByUserAndProviderAsync(
        Guid tenantId, Guid userId, IntegrationProvider provider, CancellationToken ct = default)
        => await db.Set<IntegrationConnection>()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId && x.Provider == provider, ct);

    public async Task<IReadOnlyList<IntegrationConnection>> GetByUserAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default)
        => await db.Set<IntegrationConnection>()
            .Where(x => x.TenantId == tenantId && x.UserId == userId)
            .OrderBy(x => x.Provider)
            .ToListAsync(ct);

    public async Task AddAsync(IntegrationConnection connection, CancellationToken ct = default)
        => await db.Set<IntegrationConnection>().AddAsync(connection, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}

internal sealed class OAuthTokenRepository(WorkBaseDbContext db) : IOAuthTokenRepository
{
    public async Task<OAuthToken?> GetByUserAndProviderAsync(
        Guid tenantId, Guid userId, IntegrationProvider provider, CancellationToken ct = default)
        => await db.Set<OAuthToken>()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId && x.Provider == provider, ct);

    public async Task AddAsync(OAuthToken token, CancellationToken ct = default)
        => await db.Set<OAuthToken>().AddAsync(token, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}

internal sealed class WebhookRegistrationRepository(WorkBaseDbContext db) : IWebhookRegistrationRepository
{
    public async Task<IReadOnlyList<WebhookRegistration>> GetActiveByProviderAsync(
        Guid tenantId, IntegrationProvider provider, CancellationToken ct = default)
        => await db.Set<WebhookRegistration>()
            .Where(x => x.TenantId == tenantId && x.Provider == provider && x.IsActive)
            .ToListAsync(ct);

    public async Task AddAsync(WebhookRegistration registration, CancellationToken ct = default)
        => await db.Set<WebhookRegistration>().AddAsync(registration, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
