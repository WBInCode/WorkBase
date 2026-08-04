namespace WorkBase.Contracts;

/// <summary>
/// Przekazuje powiadomienie WorkBase do czatu WB, do rozmowy od nadawcy „System”.
/// Wywołanie tylko kolejkuje zadanie w tle — niedostępny czat nie może wywrócić
/// operacji, która wywołała powiadomienie.
/// </summary>
public interface IChatNoticeForwarder
{
    void Enqueue(Guid tenantId, Guid notificationId);
}
