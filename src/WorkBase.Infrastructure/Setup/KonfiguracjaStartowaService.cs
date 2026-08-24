using Microsoft.Extensions.Caching.Memory;
using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Setup;

public sealed record StanKonfiguracji(bool Wymagana, DateTime? UkonczonaO)
{
    /// <summary>Prawda tylko wtedy, gdy firmie nalezy zablokowac reszte aplikacji.</summary>
    public bool BlokujeDostep => Wymagana && UkonczonaO is null;
}

public interface IKonfiguracjaStartowaService
{
    Task<StanKonfiguracji> PobierzAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Wolane przy TWORZENIU firmy — nigdy przy ponownej synchronizacji.</summary>
    Task OznaczJakoWymaganaAsync(Guid tenantId, CancellationToken ct = default);

    Task UkonczAsync(Guid tenantId, CancellationToken ct = default);
}

public sealed class KonfiguracjaStartowaService(
    ITenantConfigService konfiguracja,
    IMemoryCache pamiec) : IKonfiguracjaStartowaService
{
    /// <summary>
    /// Stan czytamy przy KAZDYM zadaniu, wiec bez podreczej pamieci doklada to zapytanie
    /// do bazy do calego ruchu. Czas zycia jest krotki, a ukonczenie kreatora czysci wpis
    /// od razu — uzytkownik nie czeka na wygasniecie.
    /// </summary>
    private static readonly TimeSpan CzasZycia = TimeSpan.FromMinutes(1);

    public async Task<StanKonfiguracji> PobierzAsync(Guid tenantId, CancellationToken ct = default)
    {
        if (pamiec.TryGetValue<StanKonfiguracji>(Klucz(tenantId), out var zapamietany) && zapamietany is not null)
            return zapamietany;

        var wymagana = await konfiguracja.GetAsync(tenantId, KonfiguracjaStartowa.KluczWymagana, ct);
        var ukonczona = await konfiguracja.GetAsync(tenantId, KonfiguracjaStartowa.KluczUkonczona, ct);

        var stan = new StanKonfiguracji(
            Wymagana: string.Equals(wymagana, "true", StringComparison.OrdinalIgnoreCase),
            UkonczonaO: DateTime.TryParse(ukonczona, out var data) ? data : null);

        pamiec.Set(Klucz(tenantId), stan, CzasZycia);
        return stan;
    }

    public async Task OznaczJakoWymaganaAsync(Guid tenantId, CancellationToken ct = default)
    {
        await konfiguracja.SetAsync(tenantId, KonfiguracjaStartowa.KluczWymagana, "true", ct);
        pamiec.Remove(Klucz(tenantId));
    }

    public async Task UkonczAsync(Guid tenantId, CancellationToken ct = default)
    {
        await konfiguracja.SetAsync(
            tenantId, KonfiguracjaStartowa.KluczUkonczona, DateTime.UtcNow.ToString("O"), ct);
        pamiec.Remove(Klucz(tenantId));
    }

    private static string Klucz(Guid tenantId) => $"konfiguracja-startowa:{tenantId}";
}
