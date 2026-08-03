namespace WorkBase.Contracts;

/// <summary>
/// Przekazuje powiadomienie na dzwonek w Hubie (wb-platform). Wywołanie tylko kolejkuje
/// zadanie w tle — niedostępny Hub nie może wywrócić operacji, która wywołała powiadomienie.
/// </summary>
public interface IHubNotificationForwarder
{
    void Enqueue(Guid tenantId, Guid notificationId);
}
