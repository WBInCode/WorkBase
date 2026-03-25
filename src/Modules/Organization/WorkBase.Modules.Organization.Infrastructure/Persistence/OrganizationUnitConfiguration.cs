using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Persistence;

public sealed class OrganizationUnitConfiguration : IEntityTypeConfiguration<OrganizationUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationUnit> builder)
    {
        builder.ToTable("org_units");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.TenantId)
            .IsRequired();

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.Code)
            .HasMaxLength(64);

        builder.Property(u => u.TypeId)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne<OrganizationUnitType>()
            .WithMany()
            .HasForeignKey(u => u.TypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OrganizationUnit>()
            .WithMany()
            .HasForeignKey(u => u.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.TenantId);

        builder.HasIndex(u => new { u.TenantId, u.Code })
            .IsUnique()
            .HasFilter("code IS NOT NULL");

        builder.HasIndex(u => u.ParentId);
    }
}
