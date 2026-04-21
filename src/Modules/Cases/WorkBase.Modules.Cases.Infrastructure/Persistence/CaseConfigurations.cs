using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Cases.Domain.Entities;

namespace WorkBase.Modules.Cases.Infrastructure.Persistence;

public sealed class CaseItemConfiguration : IEntityTypeConfiguration<CaseItem>
{
    public void Configure(EntityTypeBuilder<CaseItem> builder)
    {
        builder.ToTable("case_items");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Number).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.Priority).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.Number }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.AssigneeId });
        builder.HasIndex(e => new { e.TenantId, e.ContactId });
    }
}

public sealed class CaseStatusConfiguration : IEntityTypeConfiguration<CaseStatus>
{
    public void Configure(EntityTypeBuilder<CaseStatus> builder)
    {
        builder.ToTable("case_statuses");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Color).HasMaxLength(20);
        builder.HasIndex(e => e.TenantId);
    }
}

public sealed class CaseCategoryConfiguration : IEntityTypeConfiguration<CaseCategory>
{
    public void Configure(EntityTypeBuilder<CaseCategory> builder)
    {
        builder.ToTable("case_categories");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.TenantId);
    }
}

public sealed class CaseCommentConfiguration : IEntityTypeConfiguration<CaseComment>
{
    public void Configure(EntityTypeBuilder<CaseComment> builder)
    {
        builder.ToTable("case_comments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Content).IsRequired().HasMaxLength(4000);
        builder.HasIndex(e => e.CaseId);
    }
}
