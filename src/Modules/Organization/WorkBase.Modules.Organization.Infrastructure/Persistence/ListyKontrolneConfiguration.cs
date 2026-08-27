using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Persistence;

public sealed class ListaKontrolnaConfiguration : IEntityTypeConfiguration<ListaKontrolna>
{
    public void Configure(EntityTypeBuilder<ListaKontrolna> builder)
    {
        builder.ToTable("org_listy_kontrolne");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.TenantId).IsRequired();
        builder.Property(l => l.Nazwa).IsRequired().HasMaxLength(128);
        builder.Property(l => l.Wyzwalacz).IsRequired();
        builder.Property(l => l.Aktywna).IsRequired();

        // Pozycje zyja razem z lista: ladowane zawsze, kasowane razem z nia. Ten sam wzorzec
        // co dodatkowi wykonawcy zadania.
        builder.HasMany(l => l.Pozycje)
            .WithOne()
            .HasForeignKey(p => p.ListaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(l => l.Pozycje).AutoInclude().UsePropertyAccessMode(PropertyAccessMode.Field);

        // Handler pyta „aktywne listy tego wyzwalacza w tej firmie" przy kazdym przyjeciu/odejsciu.
        builder.HasIndex(l => new { l.TenantId, l.Wyzwalacz, l.Aktywna });
    }
}

public sealed class PozycjaListyKontrolnejConfiguration : IEntityTypeConfiguration<PozycjaListyKontrolnej>
{
    public void Configure(EntityTypeBuilder<PozycjaListyKontrolnej> builder)
    {
        builder.ToTable("org_listy_kontrolne_pozycje");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.ListaId).IsRequired();
        builder.Property(p => p.Tytul).IsRequired().HasMaxLength(256);
        builder.Property(p => p.DniOdZdarzenia).IsRequired();
        builder.Property(p => p.Wykonawca).IsRequired();
        builder.Property(p => p.Kolejnosc).IsRequired();
    }
}
