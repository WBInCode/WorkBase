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
            "Termin się zbliża", "{{rodzaj}} — zostało {{dni}} dni ({{data}}).", "termin_zbliza"),

        ("termin_minal", "Termin minął",
            "Termin minął", "{{rodzaj}} — termin minął {{dni}} dni temu ({{data}}).", "termin_minal"),
    ];

    public static async Task SeedTenantAsync(
        WorkBaseDbContext dbContext,
        Guid tenantId,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var istniejace = await dbContext.Set<NotificationTemplate>()
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId)
            .Select(t => t.Code)
            .ToListAsync(cancellationToken);

        var dodane = 0;
        foreach (var (kod, nazwa, tytul, tresc, kategoria) in Domyslne)
        {
            if (istniejace.Contains(kod)) continue;

            dbContext.Set<NotificationTemplate>()
                .Add(NotificationTemplate.Create(tenantId, kod, nazwa, tytul, tresc, kategoria));
            dodane++;
        }

        if (dodane == 0) return;

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Zasiano {Ile} szablonow powiadomien dla firmy {TenantId}.", dodane, tenantId);
    }
}
