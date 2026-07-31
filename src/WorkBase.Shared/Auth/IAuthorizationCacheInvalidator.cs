namespace WorkBase.Shared.Auth;

/// <summary>
/// Czysci zapamietane wyniki autoryzacji po zmianie rol lub uprawnien.
/// </summary>
/// <remarks>
/// Uprawnienia i zakresy danych sa trzymane w pamieci przez 5 minut. Bez wyczyszczenia
/// zmiana roli zaczyna dzialac dopiero po wygasnieciu wpisu, wiec administrator widzi,
/// ze "nadanie uprawnien nic nie zrobilo", i zwykle probuje ponownie.
/// </remarks>
public interface IAuthorizationCacheInvalidator
{
    /// <summary>Czysci wpisy jednego uzytkownika w danej organizacji.</summary>
    Task InvalidateUserAsync(Guid userId, Guid tenantId, CancellationToken ct = default);

    /// <summary>Czysci wpisy wszystkich uzytkownikow majacych wskazana role.</summary>
    Task InvalidateRoleAsync(Guid roleId, Guid tenantId, CancellationToken ct = default);
}
