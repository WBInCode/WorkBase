using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Persistence;

public sealed class OrganizationUnitClosureConfiguration : IEntityTypeConfiguration<OrganizationUnitClosure>
{
    public void Configure(EntityTypeBuilder<OrganizationUnitClosure> builder)
    {
        builder.ToTable("org_unit_closure");

        builder.HasKey(c => new { c.AncestorId, c.DescendantId });

        builder.Property(c => c.Depth)
            .IsRequired();

        builder.HasOne<OrganizationUnit>()
            .WithMany()
            .HasForeignKey(c => c.AncestorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<OrganizationUnit>()
            .WithMany()
            .HasForeignKey(c => c.DescendantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.AncestorId);

        builder.HasIndex(c => c.DescendantId);
    }
}
