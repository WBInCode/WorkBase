using System.Text.Json;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Domain.Entities;

public enum StatusWniosku
{
    Oczekuje = 0,
    Zaakceptowany = 1,
    Odrzucony = 2,
    Anulowany = 3,
}

/// <summary>
/// Wniosek złożony przez pracownika na formularzu zdefiniowanym przez firmę.
/// </summary>
/// <remarks>
/// Typ encji w obiegu to <see cref="TypEncjiWObiegu"/> — po nim handler domykający rozpoznaje,
/// że decyzja dotyczy wniosku, a nie urlopu czy zadania.
/// </remarks>
public sealed class Wniosek : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    /// <summary>Wartość <c>EntityType</c> instancji obiegu dla wniosków.</summary>
    public const string TypEncjiWObiegu = "Wniosek";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Guid TenantId { get; private set; }

    public Guid TypWnioskuId { get; private set; }

    /// <summary>Pracownik składający wniosek.</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>Wartości pól formularza w JSON, kluczowane kodem pola.</summary>
    public string WartosciJson { get; private set; } = "{}";

    public StatusWniosku Status { get; private set; } = StatusWniosku.Oczekuje;

    /// <summary>Instancja obiegu akceptacji. Null, gdy typ wniosku jej nie wymaga.</summary>
    public Guid? WorkflowInstanceId { get; private set; }

    public DateTime ZlozonyO { get; private set; }

    public DateTime? RozstrzygnietyO { get; private set; }

    private Wniosek() { }

    public IReadOnlyDictionary<string, string?> Wartosci()
        => JsonSerializer.Deserialize<Dictionary<string, string?>>(WartosciJson, Json) ?? [];

    public static Wniosek Zloz(
        Guid tenantId,
        Guid typWnioskuId,
        Guid employeeId,
        IReadOnlyDictionary<string, string?> wartosci,
        bool wymagaAkceptacji)
    {
        return new Wniosek
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            TypWnioskuId = typWnioskuId,
            EmployeeId = employeeId,
            WartosciJson = JsonSerializer.Serialize(wartosci, Json),
            // Wniosek niewymagajacy akceptacji jest zalatwiony w chwili zlozenia — inaczej
            // wisialby w "Oczekuje" na zawsze, bo nikt nie ma go rozstrzygnac.
            Status = wymagaAkceptacji ? StatusWniosku.Oczekuje : StatusWniosku.Zaakceptowany,
            ZlozonyO = DateTime.UtcNow,
            RozstrzygnietyO = wymagaAkceptacji ? null : DateTime.UtcNow,
        };
    }

    public void PowiazZObiegiem(Guid instanceId) => WorkflowInstanceId = instanceId;

    public Result Zaakceptuj()
    {
        if (Status != StatusWniosku.Oczekuje) return JuzRozstrzygniety();

        Status = StatusWniosku.Zaakceptowany;
        RozstrzygnietyO = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Odrzuc()
    {
        if (Status != StatusWniosku.Oczekuje) return JuzRozstrzygniety();

        Status = StatusWniosku.Odrzucony;
        RozstrzygnietyO = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Wycofanie przez samego składającego, dopóki nikt nie podjął decyzji.</summary>
    public Result Anuluj()
    {
        if (Status != StatusWniosku.Oczekuje) return JuzRozstrzygniety();

        Status = StatusWniosku.Anulowany;
        RozstrzygnietyO = DateTime.UtcNow;
        return Result.Success();
    }

    private static Result JuzRozstrzygniety() => Result.Failure(new Error(
        "Wniosek.JuzRozstrzygniety", "Ten wniosek został już rozstrzygnięty."));
}
