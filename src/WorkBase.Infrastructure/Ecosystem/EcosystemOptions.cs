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
}