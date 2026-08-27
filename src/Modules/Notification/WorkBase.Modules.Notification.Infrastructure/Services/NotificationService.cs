using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Modules.Notification.Domain.Entities;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Notification.Infrastructure.Hubs;

namespace WorkBase.Modules.Notification.Infrastructure.Services;

/// <remarks>
/// Trzy ostatnie zaleznosci maja wartosci domyslne wylacznie dla testow, ktore kanalu pocztowego
/// nie dotycza. W aplikacji wszystkie trzy sa zarejestrowane i kontener je wstrzykuje — brak
/// rejestracji skonczy sie bledem przy starcie, a nie po cichu wylaczonym mailem.
/// </remarks>
public sealed class NotificationService(
    WorkBaseDbContext db,
    IHubContext<NotificationHub> hubContext,
    IHubNotificationForwarder hubForwarder,
    IChatNoticeForwarder chatForwarder,
    IEmailSender? emailSender = null,
    IOrganizationLookupService? organizationLookup = null,
    ILogger<NotificationService>? logger = null)
    : INotificationService
{
    public async Task SendAsync(Guid tenantId, Guid recipientUserId, string title, string body,
        string category, string? referenceType = null, Guid? referenceId = null, CancellationToken ct = default)
    {
        // Preferencje istnialy jako encja, repozytorium i endpointy, ale SendAsync NIGDY ich nie
        // czytal — wysylalismy wszystko wszystkim niezaleznie od ustawien.
        //
        // Dwie domyslki i sa celowo rozne:
        // - W APLIKACJI: brak wiersza znaczy "wysylaj". Inaczej wprowadzenie preferencji
        //   uciszyloby powiadomienia wszystkim, ktorzy nigdy nic nie ustawili — czyli wszystkim.
        // - MAILEM: brak wiersza znaczy "nie wysylaj". Poczta wychodzi poza system, do skrzynki,
        //   ktorej nikt o zgode nie pytal. Wlaczenie musi byc swiadome.
        var preferencja = await db.Set<NotificationPreference>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.TenantId == tenantId
                    && p.UserId == recipientUserId
                    && p.Category == category,
                ct);
        if (preferencja is { InApp: false }) return;

        var notification = Domain.Entities.Notification.Create(
            tenantId, recipientUserId, title, body, category, referenceType, referenceId);

        await db.Set<Domain.Entities.Notification>().AddAsync(notification, ct);
        await db.SaveChangesAsync(ct);

        await hubContext.Clients.Group($"user_{recipientUserId}").SendAsync("ReceiveNotification", new
        {
            notification.Id,
            notification.Title,
            notification.Body,
            notification.Category,
            notification.CreatedAt,
            notification.ReferenceType,
            notification.ReferenceId
        }, ct);

        hubForwarder.Enqueue(tenantId, notification.Id);
        chatForwarder.Enqueue(tenantId, notification.Id);

        if (preferencja is { Email: true })
            await WyslijMailemAsync(tenantId, recipientUserId, title, body, ct);
    }

    /// <summary>
    /// Kopia powiadomienia na skrzynkę — wyłącznie dla osób, które o to poprosiły.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Awaria poczty nie może zabrać powiadomienia w aplikacji.</b> Wpis jest już zapisany,
    /// a wysyłka idzie z pętli zadań cyklicznych — nieprzechwycony wyjątek SMTP przerwałby cały
    /// przebieg i pozostali odbiorcy nie dostaliby nic. Dlatego log zamiast wyjątku.
    /// </para>
    /// <para>
    /// ponytail: wysyłka jest synchroniczna. Przy kilku adresatach to nic, przy setkach
    /// zapisanych na maile trzeba ją przenieść do kolejki, tak jak <c>IHubNotificationForwarder</c>.
    /// </para>
    /// </remarks>
    private async Task WyslijMailemAsync(
        Guid tenantId, Guid recipientUserId, string title, string body, CancellationToken ct)
    {
        if (emailSender is null || organizationLookup is null) return;

        try
        {
            var adres = await organizationLookup.GetEmailByUserIdAsync(tenantId, recipientUserId, ct);
            if (string.IsNullOrWhiteSpace(adres)) return;

            // Tresc powiadomienia niesie dane od uzytkownikow (tytul zadania, nazwisko), wiec
            // do HTML-a wchodzi po ucieczce — inaczej nawias ostry rozjechalby wiadomosc.
            var html = $"<p>{WebUtility.HtmlEncode(body)}</p>";

            await emailSender.SendAsync(adres, title, html, ct);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(
                ex, "Nie udalo sie wyslac powiadomienia mailem do konta {UserId}.", recipientUserId);
        }
    }

    /// <inheritdoc />
    public async Task SendFromTemplateAsync(
        Guid tenantId,
        Guid recipientUserId,
        string templateCode,
        IReadOnlyDictionary<string, string?> variables,
        string fallbackTitle,
        string fallbackBody,
        string category,
        string? referenceType = null,
        Guid? referenceId = null,
        CancellationToken ct = default)
    {
        // IgnoreQueryFilters, bo wysylka idzie takze z zadan cyklicznych, ktore chodza poza
        // kontekstem zadania HTTP i nie maja z czego odczytac firmy.
        var szablon = await db.Set<NotificationTemplate>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId && t.Code == templateCode && t.IsActive, ct);

        var tytul = szablon is null ? fallbackTitle : Podstaw(szablon.TitleTemplate, variables);
        var tresc = szablon is null ? fallbackBody : Podstaw(szablon.BodyTemplate, variables);

        await SendAsync(tenantId, recipientUserId, tytul, tresc, category, referenceType, referenceId, ct);
    }

    /// <summary>
    /// Podstawia <c>{{nazwa}}</c> wartościami ze słownika.
    /// </summary>
    /// <remarks>
    /// Nieznany znacznik zostaje w tekście nietknięty — autor szablonu ma zobaczyć swoją
    /// literówkę, a nie dostać w tym miejscu pustkę, której nie da się z niczym powiązać.
    /// Wartość null traktujemy jako pusty tekst: pole zostawione puste to co innego niż
    /// znacznik, którego nie znamy.
    /// </remarks>
    private static string Podstaw(string szablon, IReadOnlyDictionary<string, string?> zmienne) =>
        ZnacznikSzablonu.Replace(szablon, dopasowanie =>
        {
            var nazwa = dopasowanie.Groups[1].Value.Trim();
            return zmienne.TryGetValue(nazwa, out var wartosc) ? wartosc ?? string.Empty : dopasowanie.Value;
        });

    private static readonly Regex ZnacznikSzablonu = new(
        @"\{\{\s*([\w.-]+)\s*\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));
}
