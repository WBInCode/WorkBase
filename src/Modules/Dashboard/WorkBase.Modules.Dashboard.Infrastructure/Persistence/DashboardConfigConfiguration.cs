using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Dashboard.Domain.Entities;

namespace WorkBase.Modules.Dashboard.Infrastructure.Persistence;

public sealed class DashboardConfigConfiguration : IEntityTypeConfiguration<DashboardConfig>
{
    public void Configure(EntityTypeBuilder<DashboardConfig> builder)
    {
        builder.ToTable("dash_configs");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.UserId).IsRequired();
        builder.Property(d => d.Name).IsRequired().HasMaxLength(128);
        builder.Property(d => d.IsDefault).IsRequired();
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.ModifiedAt);

        builder.HasIndex(d => d.TenantId);
        builder.HasIndex(d => new { d.TenantId, d.UserId });
        builder.HasIndex(d => new { d.TenantId, d.UserId, d.IsDefault });
    }
}
