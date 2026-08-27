using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Documents.Domain.Entities;

namespace WorkBase.Modules.Documents.Infrastructure.Persistence;

public sealed class PotwierdzenieDokumentuConfiguration : IEntityTypeConfiguration<PotwierdzenieDokumentu>
{
    public void Configure(EntityTypeBuilder<PotwierdzenieDokumentu> builder)
    {
        builder.ToTable("doc_potwierdzenia");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.DocumentId).IsRequired();
        builder.Property(p => p.EmployeeId).IsRequired();
        builder.Property(p => p.PotwierdzonoDnia).IsRequired();

        // Jedno potwierdzenie na pare (dokument, pracownik) — pilnuje tego baza, nie kod,
        // bo dwa rownolegle klikniecia w „Zapoznalem sie" nie moga dac dwoch wierszy.
        builder.HasIndex(p => new { p.TenantId, p.DocumentId, p.EmployeeId }).IsUnique();

        // „Co mam jeszcze potwierdzic" pyta po pracowniku.
        builder.HasIndex(p => new { p.TenantId, p.EmployeeId });
    }
}
