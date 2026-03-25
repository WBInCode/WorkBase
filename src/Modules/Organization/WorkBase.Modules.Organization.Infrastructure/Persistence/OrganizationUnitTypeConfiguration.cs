using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Persistence;

public sealed class OrganizationUnitTypeConfiguration : IEntityTypeConfiguration<OrganizationUnitType>
{
    public void Configure(EntityTypeBuilder<OrganizationUnitType> builder)
    {
        builder.ToTable("org_unit_types");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TenantId)
            .IsRequired();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.Description)
            .HasMaxLength(512);

        builder.Property(t => t.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(t => t.TenantId);

        builder.HasIndex(t => new { t.TenantId, t.Name })
            .IsUnique();
    }
}
