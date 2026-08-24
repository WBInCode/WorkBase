using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

public interface IDzienWolnyRepository
{
    Task<List<DzienWolny>> PobierzZakresAsync(
        Guid tenantId, DateOnly od, DateOnly do_, CancellationToken cancellationToken = default);

    Task<DzienWolny?> PobierzAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    Task<bool> IstniejeWDniuAsync(Guid tenantId, DateOnly data, CancellationToken cancellationToken = default);

    Task DodajAsync(DzienWolny dzien, CancellationToken cancellationToken = default);

    void Usun(DzienWolny dzien);
}
