using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Forms.Domain.Entities;

namespace WorkBase.Modules.Forms.Infrastructure.Persistence;

public sealed class FormDefinitionConfiguration : IEntityTypeConfiguration<FormDefinition>
{
    public void Configure(EntityTypeBuilder<FormDefinition> builder)
    {
        builder.ToTable("forms_definitions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.WorkflowDefinitionName).HasMaxLength(256);
        builder.HasMany(e => e.Fields).WithOne().HasForeignKey(f => f.FormDefinitionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.TenantId);
    }
}

public sealed class FormFieldConfiguration : IEntityTypeConfiguration<FormField>
{
    public void Configure(EntityTypeBuilder<FormField> builder)
    {
        builder.ToTable("forms_fields");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Label).IsRequired().HasMaxLength(256);
        builder.Property(e => e.FieldType).IsRequired().HasMaxLength(32);
        builder.Property(e => e.Placeholder).HasMaxLength(512);
        builder.Property(e => e.ValidationRule).HasMaxLength(512);
        builder.Property(e => e.OptionsJson).HasColumnType("jsonb");
        builder.Property(e => e.DefaultValue).HasMaxLength(1000);
        builder.HasIndex(e => e.FormDefinitionId);
    }
}

public sealed class FormSubmissionConfiguration : IEntityTypeConfiguration<FormSubmission>
{
    public void Configure(EntityTypeBuilder<FormSubmission> builder)
    {
        builder.ToTable("forms_submissions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.ValuesJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.FormDefinitionId);
        builder.HasIndex(e => e.SubmittedBy);
    }
}
