using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Infrastructure.Persistence.Entities;

namespace WorkBase.Infrastructure.Persistence.Configurations;

public sealed class DepartmentModuleConfiguration : IEntityTypeConfiguration<DepartmentModule>
{
    public void Configure(EntityTypeBuilder<DepartmentModule> builder)
    {
        builder.ToTable("cfg_department_modules");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.ModuleType).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Description).HasMaxLength(1024);
        builder.Property(e => e.Icon).HasMaxLength(64);
        builder.Property(e => e.ConfigJson).HasColumnType("jsonb");
        builder.HasIndex(e => new { e.TenantId, e.OrgUnitId });
    }
}

public sealed class DepartmentModuleFormConfiguration : IEntityTypeConfiguration<DepartmentModuleForm>
{
    public void Configure(EntityTypeBuilder<DepartmentModuleForm> builder)
    {
        builder.ToTable("cfg_department_module_forms");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Label).HasMaxLength(256);
        builder.HasIndex(e => e.DepartmentModuleId);
    }
}

public sealed class DepartmentModuleWorkflowConfiguration : IEntityTypeConfiguration<DepartmentModuleWorkflow>
{
    public void Configure(EntityTypeBuilder<DepartmentModuleWorkflow> builder)
    {
        builder.ToTable("cfg_department_module_workflows");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Label).HasMaxLength(256);
        builder.HasIndex(e => e.DepartmentModuleId);
    }
}
