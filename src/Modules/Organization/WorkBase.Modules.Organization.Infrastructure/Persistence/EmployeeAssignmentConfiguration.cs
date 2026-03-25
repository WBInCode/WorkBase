using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Persistence;

public sealed class EmployeeAssignmentConfiguration : IEntityTypeConfiguration<EmployeeAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeAssignment> builder)
    {
        builder.ToTable("org_employee_assignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.Property(a => a.EmployeeId)
            .IsRequired();

        builder.Property(a => a.OrganizationUnitId)
            .IsRequired();

        builder.Property(a => a.PositionId)
            .IsRequired();

        builder.Property(a => a.IsPrimary)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.StartDate)
            .IsRequired();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<OrganizationUnit>()
            .WithMany()
            .HasForeignKey(a => a.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Position>()
            .WithMany()
            .HasForeignKey(a => a.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.TenantId);

        builder.HasIndex(a => a.EmployeeId);

        builder.HasIndex(a => a.OrganizationUnitId);
    }
}
