using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Modules.Workflow.Domain.Entities;
using WorkBase.Shared.Auth;

namespace WorkBase.Host.Endpoints;

/// <summary>
/// „Co jeszcze nie zadziała” — lista braków konfiguracji wyliczana z danych firmy.
/// </summary>
/// <remarks>
/// Kreator pierwszego startu zadaje trzy pytania i celowo nie pyta o resztę. Skutek jest taki,
/// że firma po kreatorze DZIAŁA, ale część funkcji jeszcze nie — i nie ma jak się o tym
/// dowiedzieć inaczej niż odkrywając to w trakcie pracy.
///
/// Dlatego każda pozycja mówi, CO NIE ZADZIAŁA, a nie czego brakuje. „Brak stanowisk
/// kierowniczych” nic nie znaczy dla nietechnicznego właściciela; „nikt nie zobaczy danych
/// swojego działu” znaczy.
///
/// Niczego nie wymuszamy. To jest informacja i skrót do ekranu, na którym da się to ustawić —
/// firma ma prawo świadomie zostawić każdą z tych rzeczy nieustawioną.
/// </remarks>
public static class GotowoscKonfiguracjiEndpoints
{
    /// <summary>Blokuje = funkcja nie zadziała wcale. Warto = zadziała, ale w okrojonej formie.</summary>
    private const string Blokuje = "blokuje";
    private const string Warto = "warto";

    public static IEndpointRouteBuilder MapGotowoscKonfiguracjiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/konfiguracja/gotowosc", async (
            ClaimsPrincipal user,
            WorkBaseDbContext db,
            CancellationToken ct) =>
        {
            var tenantId = user.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var pracownicy = await db.Set<Employee>()
                .Where(e => e.Status == EmployeeStatus.Active)
                .Select(e => new { e.Id, e.UserId, e.HourlyRate })
                .ToListAsync(ct);

            var teraz = DateTime.UtcNow;
            var majaPrzelozonego = await db.Set<SupervisorRelation>()
                .Where(r => r.StartDate <= teraz && (r.EndDate == null || r.EndDate > teraz))
                .Select(r => r.SubordinateEmployeeId)
                .Distinct()
                .ToListAsync(ct);

            var stanowiskKierowniczych = await db.Set<Position>().CountAsync(p => p.IsManagerial && p.IsActive, ct);
            var szablonowGrafiku = await db.Set<ScheduleTemplate>().CountAsync(ct);
            var politykPrzerw = await db.Set<BreakPolicy>().CountAsync(ct);
            var dniWolnych = await db.Set<DzienWolny>().CountAsync(ct);
            var typowWnioskow = await db.Set<TypWniosku>().CountAsync(t => t.Aktywny, ct);

            var bezPrzelozonego = pracownicy.Count(p => !majaPrzelozonego.Contains(p.Id));
            var bezKonta = pracownicy.Count(p => p.UserId is null);
            var bezStawki = pracownicy.Count(p => p.HourlyRate is null);

            var pozycje = new List<PozycjaGotowosci>();

            void Dodaj(bool brakuje, string kod, string tytul, string coNieZadziala, string waga, string sciezka, int? liczba = null)
            {
                if (brakuje) pozycje.Add(new PozycjaGotowosci(kod, tytul, coNieZadziala, waga, sciezka, liczba));
            }

            Dodaj(pracownicy.Count == 0, "pracownicy", "Nie ma jeszcze żadnego pracownika",
                "Bez kartotek nie da się rejestrować czasu pracy ani składać wniosków.",
                Blokuje, "/employees");

            Dodaj(bezPrzelozonego > 0 && pracownicy.Count > 1, "przelozeni", "Część osób nie ma przełożonego",
                $"Wnioski {bezPrzelozonego} osób nie mają komu trafić do akceptacji i utkną w statusie „oczekuje”.",
                Blokuje, "/employees", bezPrzelozonego);

            Dodaj(stanowiskKierowniczych == 0, "stanowiska", "Nie ma stanowiska kierowniczego",
                "Zakres danych „Dział” liczy się ze stanowisk kierowniczych — dopóki żadnego nie ma, nikt nie zobaczy danych swojego działu.",
                Blokuje, "/admin/positions");

            Dodaj(bezKonta > 0, "konta", "Część osób nie ma konta do logowania",
                $"{bezKonta} osób nie zaloguje się do systemu, dopóki nie dostaną zaproszenia.",
                Blokuje, "/employees", bezKonta);

            Dodaj(szablonowGrafiku == 0, "grafik", "Nie ma szablonu grafiku",
                "Nie ma z czego generować grafików, więc ewidencja nie będzie miała planu do porównania.",
                Blokuje, "/schedule");

            Dodaj(politykPrzerw == 0, "przerwy", "Nie ma polityki przerw",
                "Przerwy nie będą odliczane od czasu pracy.",
                Warto, "/admin/break-policies");

            Dodaj(bezStawki > 0, "stawki", "Część osób nie ma stawki godzinowej",
                $"Rozliczenie {bezStawki} osób pokaże puste kwoty — godziny policzą się normalnie.",
                Warto, "/employees", bezStawki);

            Dodaj(dniWolnych == 0, "dni-wolne", "Kalendarz dni wolnych jest pusty",
                "Dodatek świąteczny nie naliczy się, bo system nie wie, które dni są wolne.",
                Warto, "/admin/dni-wolne");

            Dodaj(typowWnioskow == 0, "typy-wnioskow", "Nie ma żadnego rodzaju wniosku",
                "Ekran „Wnioski” będzie pusty — pracownicy złożą tylko wniosek urlopowy.",
                Warto, "/admin/typy-wnioskow");

            return Results.Ok(new
            {
                blokujace = pozycje.Count(p => p.Waga == Blokuje),
                warteUwagi = pozycje.Count(p => p.Waga == Warto),
                pozycje,
            });
        })
        .WithName("PobierzGotowoscKonfiguracji")
        .WithSummary("Czego brakuje, żeby poszczególne funkcje zadziałały")
        .RequirePermission("org.edit");

        return endpoints;
    }

    private sealed record PozycjaGotowosci(
        string Kod,
        string Tytul,
        string CoNieZadziala,
        string Waga,
        string Sciezka,
        int? Liczba);
}
