using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Persistence;

public sealed class BiometricTemplateConfiguration : IEntityTypeConfiguration<BiometricTemplate>
{
    public void Configure(EntityTypeBuilder<BiometricTemplate> builder)
    {
        builder.ToTable("time_biometric_templates");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.BiometricType).IsRequired().HasMaxLength(32);
        builder.Property(e => e.TemplateHash).IsRequired().HasMaxLength(512);
        builder.HasIndex(e => new { e.TenantId, e.TemplateHash }).IsUnique();
        builder.HasIndex(e => e.EmployeeId);
    }
}

public sealed class GeofenceZoneConfiguration : IEntityTypeConfiguration<GeofenceZone>
{
    public void Configure(EntityTypeBuilder<GeofenceZone> builder)
    {
        builder.ToTable("time_geofence_zones");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Latitude).HasPrecision(10, 7);
        builder.Property(e => e.Longitude).HasPrecision(10, 7);
        builder.HasIndex(e => e.TenantId);
    }
}

public sealed class GeofenceEventConfiguration : IEntityTypeConfiguration<GeofenceEvent>
{
    public void Configure(EntityTypeBuilder<GeofenceEvent> builder)
    {
        builder.ToTable("time_geofence_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(16);
        builder.Property(e => e.Latitude).HasPrecision(10, 7);
        builder.Property(e => e.Longitude).HasPrecision(10, 7);
        builder.HasIndex(e => new { e.EmployeeId, e.OccurredAt });
    }
}
