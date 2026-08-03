namespace WorkBase.Infrastructure.Ecosystem;

public sealed class EcosystemOptions
{
    public const string SectionName = "Ecosystem";

    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
    public string HubOrgId { get; init; } = string.Empty;
    public string TimeZone { get; init; } = "Europe/Warsaw";

    /// <summary>Adres interfejsu WorkBase, uzywany do odnosnikow przy zadaniach wyslanych do Rytmu.</summary>
    public string AppUrl { get; init; } = string.Empty;

    /// <summary>Ile dni wstecz wysylac zamkniete zadania, zeby Rytm zdazyl odnotowac ich ukonczenie.</summary>
    public int ClosedTaskWindowDays { get; init; } = 30;
}