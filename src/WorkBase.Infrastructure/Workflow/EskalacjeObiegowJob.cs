using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Workflow.Domain.Entities;
using Powiadomienie = WorkBase.Modules.Notification.Domain.Entities.Notification;

namespace WorkBase.Infrastructure.Workflow;

/// <summary>
/// Egzekwowanie reguł eskalacji: wniosek, który stoi u akceptanta dłużej niż firma ustaliła.
/// </summary>
/// <remarks>
/// <para>
/// <c>EscalationRule</c> miała encję, repozytorium, komendy, endpointy <b>i gotowy ekran</b>
/// w panelu administratora — ale <b>żaden job nigdy tych reguł nie ewaluował</b>. Administrator
/// ustawiał próg czasu i nic się nie działo.
/// </para>
/// <para>
/// Klasa siedzi w <c>WorkBase.Infrastructure</c>, bo spina trzy moduły: obiegi, powiadomienia
/// i kartoteki pracowników. Ten sam wzorzec co <c>PowiadomOZaleglymZadaniu</c>.
/// </para>
/// <para>
/// <b>Powiadamiamy raz na zgłoszenie, nie raz na przebieg.</b> Job chodzi co 15 minut, a wniosek
/// potrafi stać tygodniami — bez tego zabezpieczenia jedno przeterminowane zgłoszenie dałoby
/// setki powiadomień. Ta sama pułapka wyszła już przy zaległych zadaniach i terminach kadrowych.
/// </para>
/// <para>
/// Obsługujemy wyłącznie akcję <c>notify</c>. Pozostałe dwie, które oferował ekran
/// (<c>create_task</c>, <c>update_entity</c>), są w silniku obiegów zwykłymi zaślepkami
/// logującymi zamiar — zostały z listy wyboru usunięte, żeby nie dało się skonfigurować
/// eskalacji, która nic nie robi.
/// </para>
/// </remarks>
public sealed class EskalacjeObiegowJob(
    WorkBaseDbContext dbContext,
    INotificationService powiadomienia,
    ILogger<EskalacjeObiegowJob> logger)
{
    private const string Kategoria = "escalation";
    private const string TypEncji = "approval_request";
    private const string AkcjaPowiadom = "notify";
    private const string StatusOczekujacy = "Pending";
    private const string StatusTrwajacy = "Active";

    public async Task ExecuteAsync()
    {
        var teraz = DateTime.UtcNow;

        var reguly = await dbContext.Set<EscalationRule>()
            .IgnoreQueryFilters()
            .Where(r => r.IsActive && r.ActionType == AkcjaPowiadom)
            .ToListAsync();

        if (reguly.Count == 0) return;

        var wyslane = 0;

        foreach (var regula in reguly)
        {
            var granica = teraz.AddMinutes(-regula.TimeoutMinutes);

            // Zgloszenia oczekujace dluzej niz prog, w krokach o nazwie z reguly, w instancjach
            // tej definicji obiegu. Zlaczenie po identyfikatorach, bo encje modulu nie deklaruja
            // miedzy soba wlasciwosci nawigacyjnych.
            var przeterminowane = await dbContext.Set<ApprovalRequest>()
                .IgnoreQueryFilters()
                .Where(z => z.TenantId == regula.TenantId
                    && z.Status == StatusOczekujacy
                    && z.CreatedAt <= granica)
                .Join(
                    dbContext.Set<WorkflowStep>().IgnoreQueryFilters()
                        .Where(k => k.StepName == regula.StepName),
                    z => z.StepId, k => k.Id, (z, k) => new { Zgloszenie = z, Krok = k })
                .Join(
                    dbContext.Set<WorkflowInstance>().IgnoreQueryFilters()
                        // "Active", nie "Running" — pierwsza wersja uzywala zlej nazwy i eskalacja
                        // nie odpalilaby sie NIGDY, takze na produkcji. Wylapal to test.
                        .Where(i => i.DefinitionId == regula.DefinitionId && i.Status == StatusTrwajacy),
                    x => x.Zgloszenie.InstanceId, i => i.Id, (x, i) => x.Zgloszenie)
                .ToListAsync();

            foreach (var zgloszenie in przeterminowane)
            {
                if (await JuzPowiadomionoAsync(zgloszenie)) continue;

                var odbiorca = await KontoAsync(zgloszenie.TenantId, zgloszenie.ApproverId);
                if (odbiorca is null) continue;

                var godzin = (int)Math.Floor((teraz - zgloszenie.CreatedAt).TotalHours);

                await powiadomienia.SendFromTemplateAsync(
                    zgloszenie.TenantId,
                    odbiorca.Value,
                    templateCode: Kategoria,
                    variables: new Dictionary<string, string?>
                    {
                        ["krok"] = regula.StepName,
                        ["godziny"] = godzin.ToString(),
                        ["prog"] = regula.TimeoutMinutes.ToString(),
                    },
                    fallbackTitle: "Wniosek czeka na Twoją decyzję",
                    fallbackBody: $"Sprawa „{regula.StepName}” czeka {godzin} godz. — dłużej niż ustalone {regula.TimeoutMinutes} min.",
                    category: Kategoria,
                    referenceType: TypEncji,
                    referenceId: zgloszenie.Id);

                wyslane++;
            }
        }

        if (wyslane > 0)
        {
            logger.LogInformation(
                "Eskalacje obiegow: wyslano {Ile} powiadomien z {Regul} regul.", wyslane, reguly.Count);
        }
    }

    private Task<bool> JuzPowiadomionoAsync(ApprovalRequest zgloszenie) =>
        dbContext.Set<Powiadomienie>()
            .IgnoreQueryFilters()
            .AnyAsync(p => p.TenantId == zgloszenie.TenantId
                && p.Category == Kategoria
                && p.ReferenceType == TypEncji
                && p.ReferenceId == zgloszenie.Id);

    /// <summary>Konto akceptanta. Bez konta nie ma komu wysłać.</summary>
    private async Task<Guid?> KontoAsync(Guid tenantId, Guid employeeId) =>
        await dbContext.Set<Employee>()
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.Id == employeeId && e.UserId != null)
            .Select(e => e.UserId)
            .FirstOrDefaultAsync();
}
