namespace WorkBase.Infrastructure.Security;

public sealed class ClamAvOptions
{
    public const string SectionName = "ClamAv";

    /// <summary>Wyłączenie zostawia wgrywanie bez skanowania — tylko na czas awarii skanera.</summary>
    public bool Enabled { get; init; } = true;

    public string Host { get; init; } = "wb-chat-clamav";
    public int Port { get; init; } = 3310;
    public int TimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// Co zrobić, gdy skaner nie odpowiada. Domyślnie odrzucamy plik: przepuszczenie
    /// niesprawdzonego pliku na serwer produkcyjny jest gorsze niż nieudane wgranie.
    /// </summary>
    public bool AllowUploadWhenScannerUnavailable { get; init; }
}
