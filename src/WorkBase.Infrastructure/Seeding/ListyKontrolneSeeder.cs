using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.Seeding;

/// <summary>
/// Dwa przykłady list kontrolnych dla nowej firmy — <b>wyłączone</b>.
/// </summary>
/// <remarks>
/// Pusta konfiguracja nie tłumaczy, do czego funkcja służy; włączony przykład zakładałby zadania,
/// o które nikt nie prosił. Wyłączony przykład pokazuje kształt, a włączenie to jedno kliknięcie.
/// Zasiew tylko wtedy, gdy firma nie ma jeszcze żadnej listy — po pierwszej własnej nie
/// dokładamy nic.
/// </remarks>
public static class ListyKontrolneSeeder
{
    public static async Task SeedTenantAsync(
        WorkBaseDbContext dbContext, Guid tenantId, ILogger logger, CancellationToken cancellationToken = default)
    {
        var maJakas = await dbContext.Set<ListaKontrolna>()
            .IgnoreQueryFilters()
            .AnyAsync(l => l.TenantId == tenantId, cancellationToken);
        if (maJakas) return;

        var przyjecie = ListaKontrolna.Utworz(tenantId, "Przyjęcie pracownika (przykład)", WyzwalaczListy.Przyjecie, aktywna: false);
        przyjecie.UstawPozycje(
        [
            ("Przygotuj stanowisko, sprzęt i dostępy", 0, WykonawcaPozycji.Przelozony, null),
            ("Zapoznaj się z regulaminem i instrukcją BHP", 3, WykonawcaPozycji.Pracownik, null),
            ("Rozmowa po pierwszym tygodniu", 7, WykonawcaPozycji.Przelozony, null),
        ]);

        var pozegnanie = ListaKontrolna.Utworz(tenantId, "Odejście pracownika (przykład)", WyzwalaczListy.Pozegnanie, aktywna: false);
        pozegnanie.UstawPozycje(
        [
            ("Odbierz mienie firmy (sprzęt, klucze, karta)", 0, WykonawcaPozycji.Przelozony, null),
            ("Przekaż sprawy w toku", 0, WykonawcaPozycji.Przelozony, null),
        ]);

        dbContext.AddRange(przyjecie, pozegnanie);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Zasiano 2 przykladowe listy kontrolne (wylaczone) dla firmy {TenantId}.", tenantId);
    }
}
