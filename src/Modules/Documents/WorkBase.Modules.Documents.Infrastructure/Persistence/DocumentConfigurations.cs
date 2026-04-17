using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Documents.Domain.Entities;

namespace WorkBase.Modules.Documents.Infrastructure.Persistence;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("doc_documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.FileName).IsRequired().HasMaxLength(512);
        builder.Property(d => d.StoragePath).IsRequired().HasMaxLength(1024);
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(128);
        builder.Property(d => d.FileSizeBytes).IsRequired();
        builder.Property(d => d.UploadedById).IsRequired();
        builder.Property(d => d.CategoryId);
        builder.Property(d => d.EntityType).HasMaxLength(64);
        builder.Property(d => d.EntityId);
        builder.Property(d => d.Description).HasMaxLength(1024);
        builder.Property(d => d.IsDeleted).IsRequired();
        builder.Property(d => d.DeletedAt);

        builder.HasIndex(d => d.TenantId);
        builder.HasIndex(d => new { d.TenantId, d.EntityType, d.EntityId });
        builder.HasIndex(d => new { d.TenantId, d.CategoryId });
    }
}

public sealed class DocumentCategoryConfiguration : IEntityTypeConfiguration<DocumentCategory>
{
    public void Configure(EntityTypeBuilder<DocumentCategory> builder)
    {
        builder.ToTable("doc_categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(128);
        builder.Property(c => c.Description).HasMaxLength(512);

        builder.HasIndex(c => c.TenantId);
    }
}
