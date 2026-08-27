using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using Powiadomienie = WorkBase.Modules.Notification.Domain.Entities.Notification;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.Terminy;

/// <summary>
/// Codzienne przypomnienie o terminach kadrowych: badaniach, szkoleniach BHP, uprawnieniach
/// i końcach umów.
/// </summary>
/// <remarks>
/// <para>
/// Klasa siedzi w <c>WorkBase.Infrastructure</c>, a nie w module organizacji, bo spina trzy
/// moduły naraz: terminy z Organization, powiadomienia z Notification i konta pracowników.
/// Ten sam wzorzec co <c>PowiadomOZaleglymZadaniu</c> i <c>ZamknijWniosekPoObiegu</c>.
/// </para>
/// <para>
/// <b>Powiadamiamy raz na przejście stanu, nie raz dziennie.</b> Termin siedzi w oknie
/// ostrzeżenia tygodniami, więc bez tego zabezpieczenia jedno badanie wygenerowałoby
/// trzydzieści powiadomień. Stan koduje kategoria powiadomienia, dzięki czemu ta sama pozycja
/// odzywa się dokładnie dwa razy: gdy wchodzi w okno ostrzeżenia i gdy termin faktycznie mija.
/// Ta sama pułapka wyszła wcześniej przy zaległych zadaniach.
/// </para>
/// <para>
/// Powiadamiamy pracownika i jego przełożonego. Przełożonego, bo to on umawia badania
/// i planuje zastępstwo; pracownika, bo to jego uprawnienia tracą ważność.
/// </para>
/// <para>
/// Zadanie niczego nie blokuje i nie zmienia statusu pracownika — informuje. Dopuszczenie do
/// pracy osoby z nieaktualnym badaniem jest decyzją pracodawcy, nie systemu.
/// </para>
/// </remarks>
public sealed class TerminyPrzypomnieniaJob(
    WorkBaseDbContext dbContext,
    INotificationService powiadomienia,
    ILogger<TerminyPrzypomnieniaJob> logger)
{
    private const string TypEncji = "termin";

    public async Task ExecuteAsync()
    {
        var dzisiaj = DateOnly.FromDateTime(DateTime.UtcNow);
        var teraz = DateTime.UtcNow;
        var wyslane = 0;

        // IgnoreQueryFilters, bo zadanie chodzi poza kontekstem zadania HTTP i nie ma z czego
        // odczytac firmy — filtr najemcy zwrocilby pustke dla wszystkich.
        var kandydaci = await dbContext.Set<TerminPracownika>()
            .IgnoreQueryFilters()
            .Where(t => !t.Archiwalny)
            .Join(dbContext.Set<TypTerminu>().IgnoreQueryFilters().Where(typ => typ.Aktywny),
                t => t.TypTerminuId, typ => typ.Id,
                (t, typ) => new { Termin = t, Typ = typ })
            // Nazwisko dociagniete tu, bo to samo powiadomienie idzie do pracownika I przelozonego.
            // Bez niego przelozony dostawal "Badania lekarskie — zostalo 7 dni" i nie wiedzial, czyje.
            .Join(dbContext.Set<Employee>().IgnoreQueryFilters(),
                x => x.Termin.EmployeeId, e => e.Id,
                (x, e) => new { x.Termin, x.Typ, Pracownik = e.FirstName + " " + e.LastName })
            .ToListAsync();

        foreach (var kandydat in kandydaci)
        {
            var stan = kandydat.Termin.Stan(dzisiaj, kandydat.Typ.DniOstrzezenia);
            if (stan == StanTerminu.Aktualny) continue;

            var kategoria = stan == StanTerminu.Minal ? "termin_minal" : "termin_zbliza";

            var juzWyslane = await dbContext.Set<Powiadomienie>()
                .IgnoreQueryFilters()
                .AnyAsync(p => p.TenantId == kandydat.Termin.TenantId
                    && p.Category == kategoria
                    && p.ReferenceType == TypEncji
                    && p.ReferenceId == kandydat.Termin.Id);
            if (juzWyslane) continue;

            var odbiorcy = await OdbiorcyAsync(kandydat.Termin.TenantId, kandydat.Termin.EmployeeId, teraz);
            if (odbiorcy.Count == 0) continue;

            var dni = kandydat.Termin.WaznyDo.DayNumber - dzisiaj.DayNumber;
            var tytul = stan == StanTerminu.Minal ? "Termin minął" : "Termin się zbliża";
            var tresc = stan == StanTerminu.Minal
                ? $"{kandydat.Pracownik}: {kandydat.Typ.Nazwa} — termin minął {Math.Abs(dni)} dni temu ({kandydat.Termin.WaznyDo:dd.MM.yyyy})."
                : $"{kandydat.Pracownik}: {kandydat.Typ.Nazwa} — zostało {dni} dni ({kandydat.Termin.WaznyDo:dd.MM.yyyy}).";

            var zmienne = new Dictionary<string, string?>
            {
                ["pracownik"] = kandydat.Pracownik,
                ["rodzaj"] = kandydat.Typ.Nazwa,
                ["dni"] = Math.Abs(dni).ToString(),
                ["data"] = kandydat.Termin.WaznyDo.ToString("dd.MM.yyyy"),
            };

            foreach (var odbiorca in odbiorcy)
            {
                await powiadomienia.SendFromTemplateAsync(
                    kandydat.Termin.TenantId, odbiorca,
                    templateCode: kategoria,
                    variables: zmienne,
                    fallbackTitle: tytul,
                    fallbackBody: tresc,
                    category: kategoria,
                    referenceType: TypEncji,
                    referenceId: kandydat.Termin.Id);
            }
            wyslane++;
        }

        logger.LogInformation(
            "Przypomnienia o terminach: sprawdzono {Sprawdzone}, wyslano {Wyslane}.",
            kandydaci.Count, wyslane);
    }

    /// <summary>Konta pracownika i jego aktualnego przełożonego. Bez kont nie ma komu wysłać.</summary>
    private async Task<List<Guid>> OdbiorcyAsync(Guid tenantId, Guid employeeId, DateTime teraz)
    {
        var przelozony = await dbContext.Set<SupervisorRelation>()
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId
                && r.SubordinateEmployeeId == employeeId
                && r.StartDate <= teraz
                && (r.EndDate == null || r.EndDate > teraz))
            .Select(r => (Guid?)r.SupervisorEmployeeId)
            .FirstOrDefaultAsync();

        var szukani = przelozony is Guid p ? new[] { employeeId, p } : [employeeId];

        return await dbContext.Set<Employee>()
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && szukani.Contains(e.Id) && e.UserId != null)
            .Select(e => e.UserId!.Value)
            .Distinct()
            .ToListAsync();
    }
}
