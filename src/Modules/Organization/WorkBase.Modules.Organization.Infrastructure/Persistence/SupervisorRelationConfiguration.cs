using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Persistence;

public sealed class SupervisorRelationConfiguration : IEntityTypeConfiguration<SupervisorRelation>
{
    public void Configure(EntityTypeBuilder<SupervisorRelation> builder)
    {
        builder.ToTable("org_supervisor_relations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.Property(r => r.SupervisorEmployeeId)
            .IsRequired();

        builder.Property(r => r.SubordinateEmployeeId)
            .IsRequired();

        builder.Property(r => r.StartDate)
            .IsRequired();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(r => r.SupervisorEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(r => r.SubordinateEmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.TenantId);

        builder.HasIndex(r => r.SupervisorEmployeeId);

        builder.HasIndex(r => r.SubordinateEmployeeId);

        builder.HasIndex(r => new { r.TenantId, r.SubordinateEmployeeId, r.EndDate });
    }
}
