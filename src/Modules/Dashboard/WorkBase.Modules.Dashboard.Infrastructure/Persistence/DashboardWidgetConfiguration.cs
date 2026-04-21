using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Dashboard.Domain.Entities;

namespace WorkBase.Modules.Dashboard.Infrastructure.Persistence;

public sealed class DashboardWidgetConfiguration : IEntityTypeConfiguration<DashboardWidget>
{
    public void Configure(EntityTypeBuilder<DashboardWidget> builder)
    {
        builder.ToTable("dash_widgets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.TenantId).IsRequired();
        builder.Property(w => w.DashboardConfigId).IsRequired();
        builder.Property(w => w.WidgetType).IsRequired().HasMaxLength(64);
        builder.Property(w => w.Title).IsRequired().HasMaxLength(256);
        builder.Property(w => w.Column).IsRequired();
        builder.Property(w => w.Row).IsRequired();
        builder.Property(w => w.Width).IsRequired();
        builder.Property(w => w.Height).IsRequired();
        builder.Property(w => w.Settings).HasColumnType("jsonb");
        builder.Property(w => w.IsVisible).IsRequired();
        builder.Property(w => w.SortOrder).IsRequired();

        builder.HasIndex(w => w.TenantId);
        builder.HasIndex(w => new { w.TenantId, w.DashboardConfigId });
    }
}
