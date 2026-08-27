using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Documents.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.Dokumenty;

public sealed record DokumentDoPotwierdzenia(Guid DocumentId, string FileName, string? Description, DateTime CreatedAt, int DniOdPublikacji);

public sealed record StanPotwierdzenia(Guid EmployeeId, string ImieNazwisko, DateTime? PotwierdzonoDnia, int? DniBezPotwierdzenia);

public sealed record RaportPotwierdzen(
    Guid DocumentId, string FileName, bool WymagaPotwierdzenia,
    int Potwierdzilo, int Czeka, IReadOnlyList<StanPotwierdzenia> Osoby);

/// <summary>
/// Potwierdzenie zapoznania się z dokumentem: kto ma, kto potwierdził, kto zalega.
/// </summary>
/// <remarks>
/// <para>
/// Klasa siedzi w <c>WorkBase.Infrastructure</c>, bo spina dwa moduły: dokument (Documents)
/// i listę osób, których dotyczy (Organization). Ten sam wzorzec co <c>TerminyPrzypomnieniaJob</c>.
/// </para>
/// <para>
/// <b>Kogo dotyczy dokument:</b> firmowy (bez encji) — każdego aktywnego pracownika; przypięty
/// do pracownika (<c>EntityType = "employee"</c>) — tylko jego. Dokumenty przypięte do innych
/// encji (zadania) nie mają adresata i nie da się ich oznaczyć jako wymagające potwierdzenia.
/// </para>
/// <para>
/// <b>Potwierdza wyłącznie sam pracownik.</b> Identyfikator pochodzi z tokenu. Potwierdzenie jest
/// idempotentne: drugie kliknięcie nie zmienia daty pierwszego, a wyścig dwóch kliknięć zatrzymuje
/// unikalny indeks w bazie.
/// </para>
/// </remarks>
public sealed class PotwierdzeniaDokumentow(WorkBaseDbContext db)
{
    private const string EncjaPracownika = "employee";

    /// <summary>Dokumenty, które pytający ma jeszcze potwierdzić.</summary>
    public async Task<List<DokumentDoPotwierdzenia>> DoPotwierdzeniaAsync(Guid employeeId, CancellationToken ct)
    {
        var potwierdzone = db.Set<PotwierdzenieDokumentu>()
            .Where(p => p.EmployeeId == employeeId)
            .Select(p => p.DocumentId);

        var dzis = DateTime.UtcNow;

        // Roznice dni liczymy w pamieci: EF.Functions.DateDiffDay to funkcja SQL Servera,
        // Npgsql jej nie tlumaczy. Lista jest krotka (dokumenty do potwierdzenia, nie wszystkie).
        var dokumenty = await db.Set<Document>().AsNoTracking()
            .Where(d => d.WymagaPotwierdzenia && !d.IsDeleted)
            .Where(d => d.EntityType == null || (d.EntityType == EncjaPracownika && d.EntityId == employeeId))
            .Where(d => !potwierdzone.Contains(d.Id))
            .OrderBy(d => d.CreatedAt)
            .Select(d => new { d.Id, d.FileName, d.Description, d.CreatedAt })
            .ToListAsync(ct);

        return dokumenty
            .Select(d => new DokumentDoPotwierdzenia(
                d.Id, d.FileName, d.Description, d.CreatedAt, (int)(dzis - d.CreatedAt).TotalDays))
            .ToList();
    }

    /// <summary>
    /// Składa potwierdzenie. Zwraca false, gdy dokument nie istnieje, nie wymaga potwierdzenia
    /// albo nie dotyczy tej osoby — we wszystkich trzech przypadkach odpowiedź jest ta sama,
    /// żeby nie zdradzać, czy cudzy dokument istnieje.
    /// </summary>
    public async Task<bool> PotwierdzAsync(Guid documentId, Guid employeeId, Guid tenantId, CancellationToken ct)
    {
        var dokument = await db.Set<Document>()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.WymagaPotwierdzenia && !d.IsDeleted, ct);
        if (dokument is null) return false;

        var dotyczy = dokument.EntityType is null
            || (dokument.EntityType == EncjaPracownika && dokument.EntityId == employeeId);
        if (!dotyczy) return false;

        var juz = await db.Set<PotwierdzenieDokumentu>()
            .AnyAsync(p => p.DocumentId == documentId && p.EmployeeId == employeeId, ct);
        if (juz) return true;

        db.Add(PotwierdzenieDokumentu.Zloz(tenantId, documentId, employeeId, DateTime.UtcNow));
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Flaga na dokumencie. Zwraca false, gdy dokument nie istnieje albo nie ma adresata.</summary>
    public async Task<bool> UstawWymagaAsync(Guid documentId, bool wymaga, CancellationToken ct)
    {
        var dokument = await db.Set<Document>().FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, ct);
        if (dokument is null) return false;

        // Dokument przypiety do zadania czy innej encji nie ma adresata — nie ma kto potwierdzac.
        if (wymaga && dokument.EntityType is not null && dokument.EntityType != EncjaPracownika)
            return false;

        dokument.UstawWymagaPotwierdzenia(wymaga);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Widok dla kadr: kto potwierdził, kto nie i od ilu dni.</summary>
    public async Task<RaportPotwierdzen?> RaportAsync(Guid documentId, CancellationToken ct)
    {
        var dokument = await db.Set<Document>().AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, ct);
        if (dokument is null) return null;

        var adresaci = db.Set<Employee>().AsNoTracking().Where(e => e.Status == EmployeeStatus.Active);
        if (dokument.EntityType == EncjaPracownika)
            adresaci = db.Set<Employee>().AsNoTracking().Where(e => e.Id == dokument.EntityId);
        else if (dokument.EntityType is not null)
            adresaci = db.Set<Employee>().AsNoTracking().Where(e => false);

        var osoby = await adresaci
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => new { e.Id, e.FirstName, e.LastName })
            .ToListAsync(ct);

        var potwierdzenia = await db.Set<PotwierdzenieDokumentu>().AsNoTracking()
            .Where(p => p.DocumentId == documentId)
            .ToDictionaryAsync(p => p.EmployeeId, p => p.PotwierdzonoDnia, ct);

        var dzis = DateTime.UtcNow;
        var stany = osoby.Select(o =>
        {
            var kiedy = potwierdzenia.TryGetValue(o.Id, out var data) ? data : (DateTime?)null;
            return new StanPotwierdzenia(
                o.Id, $"{o.FirstName} {o.LastName}", kiedy,
                kiedy is null ? (int)(dzis - dokument.CreatedAt).TotalDays : null);
        }).ToList();

        return new RaportPotwierdzen(
            dokument.Id, dokument.FileName, dokument.WymagaPotwierdzenia,
            stany.Count(s => s.PotwierdzonoDnia is not null),
            stany.Count(s => s.PotwierdzonoDnia is null),
            stany);
    }
}
