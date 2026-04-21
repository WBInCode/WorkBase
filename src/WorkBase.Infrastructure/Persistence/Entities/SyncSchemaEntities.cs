using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Persistence.Entities;

/// <summary>
/// Tracks offline sync operations for mobile devices.
/// Table: infra_sync_queue
/// </summary>
public sealed class SyncQueueEntry : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = default!;
    public string DeviceId { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string OperationType { get; set; } = default!; // "create", "update", "delete"
    public string PayloadJson { get; set; } = "{}";
    public long ClientTimestamp { get; set; }
    public long? ServerTimestamp { get; set; }
    public string Status { get; set; } = "pending"; // pending, synced, conflict, rejected
    public string? ConflictResolution { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
}

/// <summary>
/// Tracks tenant database schema isolation configuration.
/// Table: infra_tenant_schemas
/// </summary>
public sealed class TenantSchemaConfig
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string SchemaName { get; set; } = default!;
    public string IsolationLevel { get; set; } = "shared"; // shared, schema, database
    public string? ConnectionString { get; set; }
    public bool IsMigrated { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMigratedAt { get; set; }
}
