using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

public interface IQrTokenRepository
{
    Task<QrToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task AddAsync(QrToken qrToken, CancellationToken cancellationToken = default);
    void Update(QrToken qrToken);
}
