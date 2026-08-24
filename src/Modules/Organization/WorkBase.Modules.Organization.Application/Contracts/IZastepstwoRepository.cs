using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Application.Contracts;

public interface IZastepstwoRepository
{
    /// <summary>Zastepstwo obowiazujace w danym dniu dla wskazanej osoby, albo null.</summary>
    Task<Zastepstwo?> PobierzObowiazujaceAsync(
        Guid zastepowanyEmployeeId, DateOnly dzien, CancellationToken cancellationToken = default);

    /// <summary>Wszystkie nieodwolane zastepstwa danej osoby — do sprawdzania nakladania sie i do listy.</summary>
    Task<List<Zastepstwo>> PobierzDlaOsobyAsync(
        Guid zastepowanyEmployeeId, CancellationToken cancellationToken = default);

    Task<Zastepstwo?> PobierzAsync(Guid id, CancellationToken cancellationToken = default);

    Task DodajAsync(Zastepstwo zastepstwo, CancellationToken cancellationToken = default);
}
