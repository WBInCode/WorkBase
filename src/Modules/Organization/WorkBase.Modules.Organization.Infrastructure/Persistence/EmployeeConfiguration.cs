using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Persistence;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("org_employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(e => e.EmployeeNumber)
            .HasMaxLength(50);

        builder.Property(e => e.HireDate)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.CustomFields)
            .HasColumnType("jsonb");

        builder.Property(e => e.HourlyRate)
            .HasColumnType("numeric(10,2)");

        builder.HasIndex(e => e.CustomFields)
            .HasMethod("gin");

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => new { e.TenantId, e.Email })
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.EmployeeNumber })
            .IsUnique()
            .HasFilter("employee_number IS NOT NULL");

        builder.HasIndex(e => e.UserId);
    }
}
