namespace WorkBase.Contracts;

public interface INotificationService
{
    Task SendAsync(Guid tenantId, Guid recipientUserId, string title, string body, string category,
        string? referenceType = null, Guid? referenceId = null, CancellationToken ct = default);

    /// <summary>
    /// Wysyła powiadomienie, którego treść firma może zmienić w Ustawieniach.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ekran szablonów powiadomień istniał od dawna i zapisywał dane, ale <b>nikt nigdy nie
    /// renderował szablonu</b> — wszyscy wołający sklejali teksty na sztywno w kodzie.
    /// Administrator konfigurował i nic się nie działo.
    /// </para>
    /// <para>
    /// <paramref name="fallbackTitle"/> i <paramref name="fallbackBody"/> to teksty z kodu,
    /// używane, gdy firma nie ma szablonu o tym kodzie albo go wyłączyła. Dzięki temu skasowanie
    /// szablonu nie ucisza powiadomienia — najgorsze, co może się stać, to powrót do domyślnej treści.
    /// </para>
    /// <para>
    /// W szablonie podstawiamy <c>{{nazwa}}</c> wartościami z <paramref name="variables"/>.
    /// Nieznany znacznik zostaje w tekście, żeby autor szablonu zobaczył literówkę, zamiast
    /// dostać w tym miejscu pustkę.
    /// </para>
    /// </remarks>
    Task SendFromTemplateAsync(
        Guid tenantId,
        Guid recipientUserId,
        string templateCode,
        IReadOnlyDictionary<string, string?> variables,
        string fallbackTitle,
        string fallbackBody,
        string category,
        string? referenceType = null,
        Guid? referenceId = null,
        CancellationToken ct = default);
}
