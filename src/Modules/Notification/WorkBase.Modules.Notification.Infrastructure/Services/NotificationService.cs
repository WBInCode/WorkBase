using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkBase.Modules.Notification.Domain.Entities;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Notification.Infrastructure.Hubs;

namespace WorkBase.Modules.Notification.Infrastructure.Services;

public sealed class NotificationService(
    WorkBaseDbContext db,
    IHubContext<NotificationHub> hubContext,
    IHubNotificationForwarder hubForwarder,
    IChatNoticeForwarder chatForwarder)
    : INotificationService
{
    public async Task SendAsync(Guid tenantId, Guid recipientUserId, string title, string body,
        string category, string? referenceType = null, Guid? referenceId = null, CancellationToken ct = default)
    {
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
