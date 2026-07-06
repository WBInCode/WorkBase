using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Infrastructure.Persistence.Entities;

namespace WorkBase.Infrastructure.Persistence.Configurations;

public sealed class LicensePlanConfiguration : IEntityTypeConfiguration<LicensePlan>
{
    public void Configure(EntityTypeBuilder<LicensePlan> builder)
    {
        builder.ToTable("cfg_license_plans");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(64).IsRequired();

        // Npgsql maps string[] natively to a Postgres text[] column.
        builder.Property(x => x.IncludedModules)
            .HasColumnName("included_modules")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
