using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Workflow.Domain.Entities;
using WorkBase.Modules.Workflow.Domain.Events;

namespace WorkBase.Infrastructure.Workflow;

/// <summary>
/// Powiadomienia o akceptacjach: „coś czeka na Twoją decyzję" i „Twoja sprawa została rozpatrzona".
/// </summary>
/// <remarks>
/// <para>
/// <c>ApprovalRequestCreatedEvent</c> i <c>ApprovalDecisionMadeEvent</c> były podnoszone i żadne
/// nie miało handlera. Skutek widać było wprost na produkcji: w tabeli powiadomień istniały
/// wyłącznie trzy kategorie (anomalie i zadania) — <b>nigdy, ani razu, nikt nie dostał
/// powiadomienia o wniosku do rozpatrzenia ani o decyzji w swojej sprawie</b>. Akceptant musiał
/// sam zaglądać do kolejki, a wnioskodawca sam sprawdzać, czy coś się zmieniło.
/// </para>
/// <para>
/// Klasa siedzi w <c>WorkBase.Infrastructure</c>, bo spina trzy moduły: obiegi, powiadomienia
/// i kartoteki. Ten sam wzorzec co <c>EskalacjeObiegowJob</c> i <c>PowiadomOZaleglymZadaniu</c>.
/// </para>
/// <para>
/// <b>Bez zabezpieczenia „raz na przejście stanu".</b> To zdarzenia domenowe, a nie zadanie
/// cykliczne — powstają dokładnie raz, przy złożeniu i przy decyzji. Zabezpieczenie, którego
/// wymagają zaległe zadania i terminy, byłoby tu martwym kodem.
/// </para>
/// </remarks>
public sealed class PowiadomieniaOAkceptacjach(
    WorkBaseDbContext dbContext,
    INotificationService powiadomienia,
    ILogger<PowiadomieniaOAkceptacjach> logger)
    : INotificationHandler<ApprovalRequestCreatedEvent>,
      INotificationHandler<ApprovalDecisionMadeEvent>
{
    private const string TypEncji = "approval_request";

    /// <summary>Nazwy widoczne dla ludzi. Te same, które pokazuje kolejka akceptacji.</summary>
    private static string OpisRodzaju(string entityType) => entityType switch
    {
        "LeaveRequest" => "Wniosek urlopowy",
        "Wniosek" => "Wniosek",
        "TaskAssignment" => "Akceptacja zadania",
        _ => entityType,
    };

    public async Task Handle(ApprovalRequestCreatedEvent zdarzenie, CancellationToken ct)
    {
        // Samoakceptacja zdarza sie w firmie jednoosobowej i przy wlascicielu zatwierdzajacym
        // wlasny wniosek. Powiadomienie „masz do rozpatrzenia to, co wlasnie zlozyles" jest
        // tylko halasem.
        if (zdarzenie.ApproverId == zdarzenie.RequesterId) return;

        var konto = await KontoAsync(zdarzenie.TenantId, zdarzenie.ApproverId, ct);
        if (konto is null) return;

        var kontekst = await KontekstAsync(zdarzenie.TenantId, zdarzenie.InstanceId, zdarzenie.StepId, ct);
        var wnioskodawca = await NazwiskoAsync(zdarzenie.TenantId, zdarzenie.RequesterId, ct);

        await powiadomienia.SendFromTemplateAsync(
            zdarzenie.TenantId,
            konto.Value,
            templateCode: "approval_pending",
            variables: new Dictionary<string, string?>
            {
                ["rodzaj"] = kontekst.Rodzaj,
                ["krok"] = kontekst.Krok,
                ["wnioskodawca"] = wnioskodawca,
            },
            fallbackTitle: "Czeka na Twoją decyzję",
            fallbackBody: $"{kontekst.Rodzaj} od {wnioskodawca} — krok „{kontekst.Krok}”.",
            category: "approval_pending",
            referenceType: TypEncji,
            referenceId: zdarzenie.RequestId,
            ct: ct);

        logger.LogInformation(
            "Powiadomienie o oczekujacej decyzji: zgloszenie {RequestId}, akceptant {ApproverId}.",
            zdarzenie.RequestId, zdarzenie.ApproverId);
    }

    public async Task Handle(ApprovalDecisionMadeEvent zdarzenie, CancellationToken ct)
    {
        var zgloszenie = await dbContext.Set<ApprovalRequest>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(z => z.Id == zdarzenie.RequestId, ct);
        if (zgloszenie is null) return;

        // Kto decydowal, ten wie. Powiadomienie o wlasnej decyzji jest halasem.
        if (zgloszenie.RequesterId == zdarzenie.DecidedBy) return;

        var konto = await KontoAsync(zdarzenie.TenantId, zgloszenie.RequesterId, ct);
        if (konto is null) return;

        var kontekst = await KontekstAsync(zdarzenie.TenantId, zdarzenie.InstanceId, zgloszenie.StepId, ct);
        var decydent = await NazwiskoAsync(zdarzenie.TenantId, zdarzenie.DecidedBy, ct);
        var decyzja = OpisDecyzji(zdarzenie.Decision);

        await powiadomienia.SendFromTemplateAsync(
            zdarzenie.TenantId,
            konto.Value,
            templateCode: "approval_decided",
            variables: new Dictionary<string, string?>
            {
                ["rodzaj"] = kontekst.Rodzaj,
                ["decyzja"] = decyzja,
                ["akceptant"] = decydent,
            },
            fallbackTitle: $"{kontekst.Rodzaj}: {decyzja}",
            fallbackBody: $"{decydent} — {decyzja.ToLowerInvariant()}.",
            category: "approval_decided",
            referenceType: TypEncji,
            referenceId: zdarzenie.RequestId,
            ct: ct);
    }

    /// <remarks>
    /// Nieznana decyzja przechodzi dalej wlasnym tekstem zamiast trafiac w <c>_ =&gt; "?"</c>.
    /// Lista dopuszczalnych wartosci moze urosnac, a powiadomienie „?" nie mowi nic.
    /// </remarks>
    private static string OpisDecyzji(string decyzja) => decyzja switch
    {
        "approve" => "Zaakceptowano",
        "reject" => "Odrzucono",
        "return" => "Zwrócono do poprawy",
        _ => decyzja,
    };

    private async Task<(string Rodzaj, string Krok)> KontekstAsync(
        Guid tenantId, Guid instanceId, Guid stepId, CancellationToken ct)
    {
        var przebieg = await dbContext.Set<WorkflowInstance>()
            .IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.TenantId == tenantId && i.Id == instanceId)
            .Select(i => i.EntityType)
            .FirstOrDefaultAsync(ct);

        var krok = await dbContext.Set<WorkflowStep>()
            .IgnoreQueryFilters().AsNoTracking()
            .Where(k => k.Id == stepId)
            .Select(k => k.StepName)
            .FirstOrDefaultAsync(ct);

        return (OpisRodzaju(przebieg ?? "Sprawa"), krok ?? "akceptacja");
    }

    /// <remarks>
    /// IgnoreQueryFilters, bo handlery zdarzen chodza po zatwierdzeniu transakcji i nie zawsze
    /// maja kontekst najemcy — najemce podajemy wprost.
    /// </remarks>
    private async Task<Guid?> KontoAsync(Guid tenantId, Guid employeeId, CancellationToken ct) =>
        await dbContext.Set<Employee>()
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.Id == employeeId && e.UserId != null)
            .Select(e => e.UserId)
            .FirstOrDefaultAsync(ct);

    private async Task<string> NazwiskoAsync(Guid tenantId, Guid employeeId, CancellationToken ct) =>
        await dbContext.Set<Employee>()
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.Id == employeeId)
            .Select(e => e.FirstName + " " + e.LastName)
            .FirstOrDefaultAsync(ct) ?? "pracownik";
}
