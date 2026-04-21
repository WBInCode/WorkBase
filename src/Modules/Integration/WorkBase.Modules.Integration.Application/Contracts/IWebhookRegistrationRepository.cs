using WorkBase.Modules.Integration.Domain.Entities;
using WorkBase.Modules.Integration.Domain.Enums;

namespace WorkBase.Modules.Integration.Application.Contracts;

public interface IWebhookRegistrationRepository
{
    Task<IReadOnlyList<WebhookRegistration>> GetActiveByProviderAsync(Guid tenantId, IntegrationProvider provider, CancellationToken ct = default);
    Task AddAsync(WebhookRegistration registration, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
