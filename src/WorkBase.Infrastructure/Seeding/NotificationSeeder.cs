using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Notification.Domain.Entities;

namespace WorkBase.Infrastructure.Seeding;

/// <summary>
/// Startowe szablony powiadomień dla nowej firmy.
/// </summary>
/// <remarks>
/// <para>
/// Ekran szablonów istniał od dawna i zapisywał dane, ale nikt ich nie renderował, więc lista
/// była pusta i pozostawała pusta — administrator nie miał nawet jak się dowiedzieć, jakie kody
/// szablonów system rozpoznaje.
/// </para>
/// <para>
/// Zasiewamy więc szablony <b>odwzorowujące dzisiejsze teksty z kodu</b>. Firma widzi, co
/// system wysyła, i może to przepisać pod siebie. Skasowanie szablonu nie ucisza powiadomienia:
/// wysyłka wraca wtedy do tekstu awaryjnego z kodu.
/// </para>
/// </remarks>
public static class NotificationSeeder
{
    private static readonly (string Kod, string Nazwa, string Tytul, string Tresc, string Kategoria)[] Domyslne =
    [
        ("task_assigned", "Przydzielone zadanie",
            "Nowe zadanie", "{{tytul}}", "task_assigned"),

        ("task_overdue", "Zadanie po terminie",
            "Zadanie po terminie", "{{tytul}} — {{opis}}", "task_overdue"),

        ("anomaly_detected", "Wykryta anomalia czasu pracy",
            "Anomalia: {{rodzaj}}", "{{pracownik}}: {{rodzaj}} w dniu {{data}}.", "anomaly_detected"),

        ("termin_zbliza", "Zbliżający się termin",
            "Termin się zbliża", "{{pracownik}}: {{rodzaj}} — zostało {{dni}} dni ({{data}}).", "termin_zbliza"),

        ("termin_minal", "Termin minął",
            "Termin minął", "{{pracownik}}: {{rodzaj}} — termin minął {{dni}} dni temu ({{data}}).", "termin_minal"),

        ("escalation", "Wniosek czeka na decyzję",
            "Wniosek czeka na Twoją decyzję",
            "Sprawa „{{krok}}” czeka {{godziny}} godz. — dłużej niż ustalone {{prog}} min.", "escalation"),
    ];

    /// <summary>
    /// Wcześniejsze wersje treści domyślnej, do których dopisujemy poprawkę.
    /// </summary>
    /// <remarks>
    /// Przypomnienie o terminie idzie do pracownika <b>i</b> jego przełożonego, ale pierwsza wersja
    /// szablonu nie mówiła, CZYJ to termin — przełożony dostawał „Badania lekarskie — zostało 10 dni".
    /// Tekst awaryjny w kodzie już to naprawia, tylko że aktywny szablon go przesłania, więc firmy
    /// zasiane wcześniej zostałyby z wadą na zawsze.
    ///
    /// Podmieniamy <b>wyłącznie treść identyczną ze starym ziarnem</b>. Szablon, którego firma
    /// dotknęła, jest jej decyzją i zostaje nietknięty.
    /// </remarks>
    private static readonly (string Kod, string StaraTresc)[] DoPoprawki =
    [
        ("termin_zbliza", "{{rodzaj}} — zostało {{dni}} dni ({{data}})."),
        ("termin_minal", "{{rodzaj}} — termin minął {{dni}} dni temu ({{data}})."),
    ];

    public static async Task SeedTenantAsync(
        WorkBaseDbContext dbContext,
        Guid tenantId,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var szablony = await dbContext.Set<NotificationTemplate>()
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var istniejace = szablony.Select(t => t.Code).ToList();

        var poprawione = 0;
        foreach (var (kod, staraTresc) in DoPoprawki)
        {
            var szablon = szablony.FirstOrDefault(t => t.Code == kod && t.BodyTemplate == staraTresc);
            if (szablon is null) continue;

            var nowa = Domyslne.First(d => d.Kod == kod);
            szablon.Update(nowa.Nazwa, nowa.Tytul, nowa.Tresc, nowa.Kategoria);
            poprawione++;
        }

        var dodane = 0;
        foreach (var (kod, nazwa, tytul, tresc, kategoria) in Domyslne)
        {
            if (istniejace.Contains(kod)) continue;

            dbContext.Set<NotificationTemplate>()
                .Add(NotificationTemplate.Create(tenantId, kod, nazwa, tytul, tresc, kategoria));
            dodane++;
        }

        if (dodane == 0 && poprawione == 0) return;

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Szablony powiadomien firmy {TenantId}: zasiano {Zasiane}, poprawiono {Poprawione}.",
            tenantId, dodane, poprawione);
    }
}
