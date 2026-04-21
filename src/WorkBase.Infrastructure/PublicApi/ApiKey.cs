using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.PublicApi;

public sealed class ApiKey : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string KeyHash { get; private set; } = null!; // SHA256 hash of the actual key
    public string KeyPrefix { get; private set; } = null!; // first 8 chars for identification
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string? AllowedIps { get; private set; } // comma-separated
    public string? ScopesJson { get; private set; } // JSON: ["employees.read","time.read"]
    public int RateLimitPerMinute { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private ApiKey() { }

    public static ApiKey Create(
        Guid tenantId, string name, string keyHash, string keyPrefix,
        string? description = null, DateTime? expiresAt = null,
        string? allowedIps = null, string? scopesJson = null,
        int rateLimitPerMinute = 60)
    {
        return new ApiKey
        {
            TenantId = tenantId,
            Name = name,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Description = description,
            IsActive = true,
            ExpiresAt = expiresAt,
            AllowedIps = allowedIps,
            ScopesJson = scopesJson,
            RateLimitPerMinute = rateLimitPerMinute,
        };
    }

    public void Update(string name, string? description, string? allowedIps, string? scopesJson, int rateLimitPerMinute)
    {
        Name = name; Description = description; AllowedIps = allowedIps;
        ScopesJson = scopesJson; RateLimitPerMinute = rateLimitPerMinute;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void RecordUsage() => LastUsedAt = DateTime.UtcNow;
}
