using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Dashboard.Domain.Entities;

namespace WorkBase.Modules.Dashboard.Infrastructure.Persistence;

public sealed class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.ToTable("dashboard_reports");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.ReportType).IsRequired().HasMaxLength(32);
        builder.Property(e => e.DataSource).IsRequired().HasMaxLength(64);
        builder.Property(e => e.FiltersJson).HasColumnType("jsonb");
        builder.Property(e => e.ColumnsJson).HasColumnType("jsonb");
        builder.Property(e => e.GroupByJson).HasColumnType("jsonb");
        builder.Property(e => e.AggregationsJson).HasColumnType("jsonb");
        builder.Property(e => e.ChartConfigJson).HasColumnType("jsonb");
        builder.Property(e => e.SortJson).HasColumnType("jsonb");
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.CreatedByUserId);
    }
}
