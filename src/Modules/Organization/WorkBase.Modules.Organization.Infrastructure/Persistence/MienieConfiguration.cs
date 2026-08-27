using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Persistence;

public sealed class MieniePowierzoneConfiguration : IEntityTypeConfiguration<MieniePowierzone>
{
    public void Configure(EntityTypeBuilder<MieniePowierzone> builder)
    {
        builder.ToTable("org_mienie_powierzone");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.EmployeeId).IsRequired();
        builder.Property(m => m.Rodzaj).IsRequired().HasMaxLength(64);
        builder.Property(m => m.Nazwa).IsRequired().HasMaxLength(256);
        builder.Property(m => m.NumerSeryjny).HasMaxLength(128);
        builder.Property(m => m.Wartosc).HasPrecision(12, 2);
        builder.Property(m => m.WydanoDnia).IsRequired();
        builder.Property(m => m.Notatka).HasMaxLength(1024);

        // Jeden indeks czesciowy zamiast dwoch: karta pracownika, licznik przy dezaktywacji
        // i lista „do zwrotu" pytaja o NIEZWROCONE, a tych jest ulamek historii. Pelna historia
        // (ze zwroconymi) to rzadki odczyt i moze isc po tabeli. Dwa HasIndex na tych samych
        // kolumnach EF i tak zlewa w jeden, wiec drugi bylby zludzeniem.
        builder.HasIndex(m => new { m.TenantId, m.EmployeeId })
            .HasDatabaseName("ix_org_mienie_powierzone_niezwrocone")
            .HasFilter("zwrocono_dnia IS NULL");
    }
}
