using WorkBase.Modules.Integration.Domain.Entities;
using WorkBase.Modules.Integration.Domain.Enums;

namespace WorkBase.Modules.Integration.Application.Contracts;

public interface IOAuthTokenRepository
{
    Task<OAuthToken?> GetByUserAndProviderAsync(Guid tenantId, Guid userId, IntegrationProvider provider, CancellationToken ct = default);
    Task AddAsync(OAuthToken token, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
