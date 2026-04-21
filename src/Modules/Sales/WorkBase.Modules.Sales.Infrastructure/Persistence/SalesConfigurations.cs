using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Sales.Domain.Entities;

namespace WorkBase.Modules.Sales.Infrastructure.Persistence;

public sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("sales_leads");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.CompanyName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.ContactName).HasMaxLength(256);
        builder.Property(e => e.Email).HasMaxLength(256);
        builder.Property(e => e.Phone).HasMaxLength(64);
        builder.Property(e => e.Source).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(e => new { e.TenantId, e.Status });
    }
}

public sealed class OpportunityConfiguration : IEntityTypeConfiguration<Opportunity>
{
    public void Configure(EntityTypeBuilder<Opportunity> builder)
    {
        builder.ToTable("sales_opportunities");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Stage).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Value).HasPrecision(18, 2);
        builder.Property(e => e.Currency).HasMaxLength(8);
        builder.Property(e => e.LostReason).HasMaxLength(512);
        builder.HasIndex(e => new { e.TenantId, e.Stage });
    }
}

public sealed class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("sales_offers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Number).IsRequired().HasMaxLength(64);
        builder.Property(e => e.Title).HasMaxLength(256);
        builder.Property(e => e.TotalNet).HasPrecision(18, 2);
        builder.Property(e => e.TotalGross).HasPrecision(18, 2);
        builder.Property(e => e.Currency).HasMaxLength(8);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.ItemsJson).HasColumnType("jsonb");
        builder.HasIndex(e => e.OpportunityId);
    }
}

public sealed class PipelineStageConfiguration : IEntityTypeConfiguration<PipelineStage>
{
    public void Configure(EntityTypeBuilder<PipelineStage> builder)
    {
        builder.ToTable("sales_pipeline_stages");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Color).HasMaxLength(16);
        builder.HasIndex(e => new { e.TenantId, e.SortOrder });
    }
}
