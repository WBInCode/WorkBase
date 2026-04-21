using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Infrastructure.Persistence.Entities;

namespace WorkBase.Infrastructure.Persistence.Configurations;

public sealed class SyncQueueEntryConfiguration : IEntityTypeConfiguration<SyncQueueEntry>
{
    public void Configure(EntityTypeBuilder<SyncQueueEntry> builder)
    {
        builder.ToTable("infra_sync_queue");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(256);
        builder.Property(e => e.DeviceId).IsRequired().HasMaxLength(256);
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(128);
        builder.Property(e => e.EntityId).IsRequired().HasMaxLength(128);
        builder.Property(e => e.OperationType).IsRequired().HasMaxLength(16);
        builder.Property(e => e.PayloadJson).HasColumnType("jsonb");
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.ConflictResolution).HasMaxLength(512);
        builder.HasIndex(e => new { e.TenantId, e.UserId, e.Status });
        builder.HasIndex(e => new { e.DeviceId, e.ClientTimestamp });
    }
}

public sealed class TenantSchemaConfigConfiguration : IEntityTypeConfiguration<TenantSchemaConfig>
{
    public void Configure(EntityTypeBuilder<TenantSchemaConfig> builder)
    {
        builder.ToTable("infra_tenant_schemas");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.SchemaName).IsRequired().HasMaxLength(128);
        builder.Property(e => e.IsolationLevel).IsRequired().HasMaxLength(16);
        builder.Property(e => e.ConnectionString).HasMaxLength(1024);
        builder.HasIndex(e => e.TenantId).IsUnique();
        builder.HasIndex(e => e.SchemaName).IsUnique();
    }
}
