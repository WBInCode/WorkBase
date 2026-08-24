using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Persistence;

public sealed class DzienWolnyConfiguration : IEntityTypeConfiguration<DzienWolny>
{
    public void Configure(EntityTypeBuilder<DzienWolny> builder)
    {
        builder.ToTable("time_dni_wolne");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.Data).IsRequired();
        builder.Property(d => d.Nazwa).IsRequired().HasMaxLength(128);

        // Nazwa poziomu, nie liczba — kolumna trzyma tekst, a rzutowanie na int w projekcji
        // EF tlumaczy na scope_level::int i baza odrzuca zapytanie (ten sam blad 22P02, ktory
        // wystapil juz przy zakresach danych).
        builder.Property(d => d.Rodzaj).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Property(d => d.ObnizaNorme).IsRequired();

        // Jeden dzien wolny na date w firmie — inaczej ta sama data obnizalaby norme dwa razy.
        builder.HasIndex(d => new { d.TenantId, d.Data }).IsUnique();
    }
}
