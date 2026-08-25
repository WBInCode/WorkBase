using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Persistence;

public sealed class TypWnioskuConfiguration : IEntityTypeConfiguration<TypWniosku>
{
    public void Configure(EntityTypeBuilder<TypWniosku> builder)
    {
        builder.ToTable("wf_typy_wnioskow");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.Kod).IsRequired().HasMaxLength(64);
        builder.Property(t => t.Nazwa).IsRequired().HasMaxLength(128);
        builder.Property(t => t.Opis).HasMaxLength(512);
        builder.Property(t => t.PolaJson).IsRequired().HasColumnType("jsonb");
        builder.Property(t => t.WymagaAkceptacji).IsRequired();
        builder.Property(t => t.Aktywny).IsRequired();

        // Kod jest stalym identyfikatorem typu w obrebie firmy — dwa typy o tym samym kodzie
        // byloby nie do rozroznienia na liscie i w raportach.
        builder.HasIndex(t => new { t.TenantId, t.Kod }).IsUnique();
    }
}

public sealed class WniosekConfiguration : IEntityTypeConfiguration<Wniosek>
{
    public void Configure(EntityTypeBuilder<Wniosek> builder)
    {
        builder.ToTable("wf_wnioski");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.TenantId).IsRequired();
        builder.Property(w => w.TypWnioskuId).IsRequired();
        builder.Property(w => w.EmployeeId).IsRequired();
        builder.Property(w => w.WartosciJson).IsRequired().HasColumnType("jsonb");
        builder.Property(w => w.ZlozonyO).IsRequired();

        // Nazwa statusu, nie liczba — kolumna trzyma tekst, a rzutowanie na int w projekcji
        // EF tlumaczy na status::int i baza odrzuca zapytanie bledem 22P02.
        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasOne<TypWniosku>()
            .WithMany()
            .HasForeignKey(w => w.TypWnioskuId)
            .OnDelete(DeleteBehavior.Restrict);

        // Dwa zapytania na sciezce uzytkownika: "moje wnioski" i "wnioski firmy wg statusu".
        builder.HasIndex(w => new { w.TenantId, w.EmployeeId, w.ZlozonyO });
        builder.HasIndex(w => new { w.TenantId, w.Status });
        builder.HasIndex(w => w.WorkflowInstanceId);
    }
}
