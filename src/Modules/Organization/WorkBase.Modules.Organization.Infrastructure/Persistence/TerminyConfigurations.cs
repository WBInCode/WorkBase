using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Persistence;

public sealed class TypTerminuConfiguration : IEntityTypeConfiguration<TypTerminu>
{
    public void Configure(EntityTypeBuilder<TypTerminu> builder)
    {
        builder.ToTable("org_typy_terminow");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.Kod).IsRequired().HasMaxLength(64);
        builder.Property(t => t.Nazwa).IsRequired().HasMaxLength(128);
        builder.Property(t => t.Opis).HasMaxLength(512);
        builder.Property(t => t.DniOstrzezenia).IsRequired();
        builder.Property(t => t.Aktywny).IsRequired();

        // Kod identyfikuje rodzaj przy zasiewie i imporcie, wiec musi byc jednoznaczny w firmie.
        builder.HasIndex(t => new { t.TenantId, t.Kod }).IsUnique();
    }
}

public sealed class TerminPracownikaConfiguration : IEntityTypeConfiguration<TerminPracownika>
{
    public void Configure(EntityTypeBuilder<TerminPracownika> builder)
    {
        builder.ToTable("org_terminy_pracownikow");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.EmployeeId).IsRequired();
        builder.Property(t => t.TypTerminuId).IsRequired();
        builder.Property(t => t.WaznyDo).IsRequired();
        builder.Property(t => t.Notatka).HasMaxLength(512);
        builder.Property(t => t.Archiwalny).IsRequired();

        // Zapytanie „co wygasa w najblizszych N dniach" filtruje po dacie i pomija archiwalne,
        // a idzie po calej firmie — bez tego indeksu skanowaloby cala tabele przy kazdym
        // wejsciu na liste i przy kazdym przebiegu zadania cyklicznego.
        builder.HasIndex(t => new { t.TenantId, t.Archiwalny, t.WaznyDo });

        // Karta pracownika pokazuje jego terminy — osobny indeks, bo to zupelnie inny dostep.
        builder.HasIndex(t => new { t.TenantId, t.EmployeeId });
    }
}
