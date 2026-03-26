using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Identity.Domain.Entities;

namespace WorkBase.Modules.Identity.Infrastructure.Persistence;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("iam_roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(r => r.Description)
            .HasMaxLength(512);

        builder.Property(r => r.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.Level)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(r => r.TenantId);

        builder.HasIndex(r => new { r.TenantId, r.Name })
            .IsUnique();

        builder.Ignore(r => r.DomainEvents);
    }
}
