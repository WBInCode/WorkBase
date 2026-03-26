using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Identity.Domain.Entities;

namespace WorkBase.Modules.Identity.Infrastructure.Persistence;

public sealed class DataScopeConfiguration : IEntityTypeConfiguration<DataScope>
{
    public void Configure(EntityTypeBuilder<DataScope> builder)
    {
        builder.ToTable("iam_data_scopes");

        builder.HasKey(ds => ds.Id);

        builder.Property(ds => ds.TenantId)
            .IsRequired();

        builder.Property(ds => ds.Module)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(ds => ds.ScopeLevel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(ds => ds.CustomFilter)
            .HasColumnType("jsonb");

        builder.HasOne(ds => ds.Role)
            .WithMany()
            .HasForeignKey(ds => ds.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ds => ds.TenantId);

        builder.HasIndex(ds => new { ds.RoleId, ds.Module })
            .IsUnique();

        builder.Ignore(ds => ds.DomainEvents);
    }
}
