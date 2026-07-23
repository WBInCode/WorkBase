using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Persistence;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("org_tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.Slug)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.Settings)
            .HasColumnType("jsonb");

        builder.Property(t => t.HubOrganizationId)
            .HasMaxLength(128);

        builder.Property(t => t.HubProductInstanceId)
            .HasMaxLength(128);

        builder.Property(t => t.KeycloakRealmName)
            .HasMaxLength(128);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(TenantStatus.Active);

        builder.HasIndex(t => t.Slug)
            .IsUnique();

        builder.HasIndex(t => t.HubOrganizationId)
            .IsUnique();

        builder.HasIndex(t => t.HubProductInstanceId)
            .IsUnique();

        builder.HasIndex(t => t.KeycloakRealmName)
            .IsUnique();
    }
}
