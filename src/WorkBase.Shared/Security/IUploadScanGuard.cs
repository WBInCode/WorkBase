using WorkBase.Shared.Domain;

namespace WorkBase.Shared.Security;

/// <summary>
/// Bramka antywirusowa dla wgrywanych plików. Zwraca wynik nieudany, gdy plik jest zakażony
/// albo gdy skanera nie da się zapytać, a konfiguracja nie pozwala przepuścić niesprawdzonego pliku.
/// </summary>
public interface IUploadScanGuard
{
    Task<Result> InspectAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
}
