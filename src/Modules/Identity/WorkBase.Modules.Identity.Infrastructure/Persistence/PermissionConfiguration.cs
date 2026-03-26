using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Identity.Domain.Entities;

namespace WorkBase.Modules.Identity.Infrastructure.Persistence;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("iam_permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Module)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(p => p.Action)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(p => p.Scope)
            .HasMaxLength(64);

        builder.Property(p => p.Description)
            .HasMaxLength(512);

        builder.HasIndex(p => new { p.Module, p.Action, p.Scope })
            .IsUnique();

        builder.Ignore(p => p.FullCode);
        builder.Ignore(p => p.DomainEvents);
    }
}
