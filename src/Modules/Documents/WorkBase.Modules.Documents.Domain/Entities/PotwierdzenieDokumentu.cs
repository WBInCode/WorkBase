using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Documents.Domain.Entities;

/// <summary>
/// Ślad, że pracownik zapoznał się z dokumentem: regulaminem, instrukcją BHP, polityką.
/// </summary>
/// <remarks>
/// <para>
/// Jeden wiersz na parę (dokument, pracownik). Potwierdzenie składa <b>wyłącznie sam pracownik</b>
/// ze swojego konta — kadry mogą oznaczyć dokument jako wymagający potwierdzenia, ale nie mogą
/// potwierdzić za kogoś, bo wtedy ślad nic by nie znaczył przy kontroli.
/// </para>
/// <para>
/// Nie ma tu „cofnięcia": kto raz potwierdził, potwierdził. Nowa wersja regulaminu to nowy
/// dokument i nowe potwierdzenia — tak jak w segregatorze z podpisami.
/// </para>
/// </remarks>
public sealed class PotwierdzenieDokumentu : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid DocumentId { get; private set; }

    public Guid EmployeeId { get; private set; }

    public DateTime PotwierdzonoDnia { get; private set; }

    private PotwierdzenieDokumentu() { }

    public static PotwierdzenieDokumentu Zloz(Guid tenantId, Guid documentId, Guid employeeId, DateTime teraz) =>
        new()
        {
            TenantId = tenantId,
            DocumentId = documentId,
            EmployeeId = employeeId,
            PotwierdzonoDnia = teraz,
        };
}
