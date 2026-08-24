using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Persistence;

public sealed class ZastepstwoConfiguration : IEntityTypeConfiguration<Zastepstwo>
{
    public void Configure(EntityTypeBuilder<Zastepstwo> builder)
    {
        builder.ToTable("org_zastepstwa");

        builder.HasKey(z => z.Id);

        builder.Property(z => z.TenantId).IsRequired();
        builder.Property(z => z.ZastepowanyEmployeeId).IsRequired();
        builder.Property(z => z.ZastepcaEmployeeId).IsRequired();
        builder.Property(z => z.OdKiedy).IsRequired();
        builder.Property(z => z.DoKiedy).IsRequired();
        builder.Property(z => z.Powod).HasMaxLength(256);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(z => z.ZastepowanyEmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(z => z.ZastepcaEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Wyszukiwanie zawsze idzie po zastepowanym i dacie — to jedyne zapytanie na sciezce
        // wyznaczania akceptanta, wiec ma trafiac w indeks.
        builder.HasIndex(z => new { z.TenantId, z.ZastepowanyEmployeeId, z.OdKiedy, z.DoKiedy });
    }
}
