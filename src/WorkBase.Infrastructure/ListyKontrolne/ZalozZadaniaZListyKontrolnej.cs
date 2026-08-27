using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Events;
using WorkBase.Modules.Tasks.Domain.Entities;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Infrastructure.ListyKontrolne;

/// <summary>
/// Przy przyjęciu albo odejściu pracownika zakłada zadania z aktywnych list kontrolnych.
/// </summary>
/// <remarks>
/// <para>
/// Klasa siedzi w <c>WorkBase.Infrastructure</c>, bo spina dwa moduły: zdarzenia i listy
/// z Organization oraz zadania z Tasks. Ten sam wzorzec co <c>PowiadomOZaleglymZadaniu</c>;
/// rejestracja ręczna w <c>AddWorkBaseInfrastructure</c>, czego pilnuje
/// <c>HandleryZdarzenSaZarejestrowaneTests</c>.
/// </para>
/// <para>
/// <b>Bez zabezpieczenia „raz na zdarzenie".</b> To handlery zdarzeń domenowych, które powstają
/// dokładnie raz — przy utworzeniu i przy dezaktywacji. Przywrócenie pracownika
/// (<c>EmployeeActivatedEvent</c>) celowo NIE uruchamia listy przyjęcia: to powrót, nie przyjęcie.
/// </para>
/// <para>
/// Pozycja, której wykonawcy nie da się ustalić (przełożony nieustawiony, wskazana osoba
/// nieaktywna), jest pomijana z wpisem w logu — nie przerywa reszty listy i nie wywraca
/// zapisu w kadrach, który już się dokonał.
/// </para>
/// </remarks>
public sealed class ZalozZadaniaZListyKontrolnej(
    WorkBaseDbContext db,
    ILogger<ZalozZadaniaZListyKontrolnej> logger)
    : INotificationHandler<EmployeeCreatedEvent>,
      INotificationHandler<EmployeeDeactivatedEvent>
{
    public Task Handle(EmployeeCreatedEvent zdarzenie, CancellationToken ct) =>
        UruchomAsync(zdarzenie.TenantId, zdarzenie.EmployeeId, WyzwalaczListy.Przyjecie, ct);

    public Task Handle(EmployeeDeactivatedEvent zdarzenie, CancellationToken ct) =>
        UruchomAsync(zdarzenie.TenantId, zdarzenie.EmployeeId, WyzwalaczListy.Pozegnanie, ct);

    private async Task UruchomAsync(Guid tenantId, Guid employeeId, WyzwalaczListy wyzwalacz, CancellationToken ct)
    {
        try
        {
            // IgnoreQueryFilters: handler chodzi po zatwierdzeniu transakcji, a przy imporcie
            // z Huba bywa poza kontekstem HTTP — najemce podajemy wprost.
            var listy = await db.Set<ListaKontrolna>()
                .IgnoreQueryFilters()
                .Where(l => l.TenantId == tenantId && l.Wyzwalacz == wyzwalacz && l.Aktywna)
                .ToListAsync(ct);
            if (listy.Count == 0) return;

            var pracownik = await db.Set<Employee>()
                .IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId && e.TenantId == tenantId, ct);
            if (pracownik is null) return;

            var statusId = await db.Set<TaskStatus>()
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && s.IsActive)
                .OrderByDescending(s => s.IsDefault).ThenBy(s => s.SortOrder)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);

            // Priorytet „normalny" jesli firma go ma; inaczej pierwszy z listy. Bez priorytetu
            // TaskItem nie powstanie, wiec brak slownika = brak zadan, z wpisem w logu.
            var priorytetId = await db.Set<TaskPriority>()
                .IgnoreQueryFilters()
                .Where(p => p.TenantId == tenantId && p.IsActive)
                .OrderBy(p => p.Code == "NORMAL" ? 0 : 1).ThenBy(p => p.SortOrder)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(ct);

            if (statusId is null || priorytetId is null)
            {
                logger.LogWarning(
                    "Lista kontrolna ({Wyzwalacz}) dla {EmployeeId}: firma {TenantId} nie ma statusu lub priorytetu zadan — pomijam.",
                    wyzwalacz, employeeId, tenantId);
                return;
            }

            var teraz = DateTime.UtcNow;
            var przelozonyId = await PrzelozonyAsync(tenantId, employeeId, teraz, ct);
            var zalozone = 0;

            foreach (var lista in listy)
            foreach (var pozycja in lista.Pozycje.OrderBy(p => p.Kolejnosc))
            {
                var wykonawca = pozycja.Wykonawca switch
                {
                    WykonawcaPozycji.Pracownik => employeeId,
                    WykonawcaPozycji.Przelozony => przelozonyId,
                    WykonawcaPozycji.Osoba => pozycja.OsobaId,
                    _ => null,
                };

                if (wykonawca is null)
                {
                    logger.LogWarning(
                        "Lista „{Lista}”, pozycja „{Pozycja}”: brak wykonawcy ({Wykonawca}) dla pracownika {EmployeeId} — pomijam.",
                        lista.Nazwa, pozycja.Tytul, pozycja.Wykonawca, employeeId);
                    continue;
                }

                db.Add(TaskItem.Create(
                    tenantId,
                    pozycja.Tytul,
                    statusId.Value,
                    priorytetId.Value,
                    wykonawca.Value,
                    reporterId: null,
                    description: $"Lista kontrolna „{lista.Nazwa}” — {pracownik.FirstName} {pracownik.LastName}.",
                    dueDate: teraz.Date.AddDays(pozycja.DniOdZdarzenia)));
                zalozone++;
            }

            if (zalozone == 0) return;

            await db.SaveChangesAsync(ct);
            logger.LogInformation(
                "Lista kontrolna ({Wyzwalacz}) dla {EmployeeId}: zalozono {Ile} zadan z {List} list.",
                wyzwalacz, employeeId, zalozone, listy.Count);
        }
        catch (Exception ex)
        {
            // Zapis w kadrach juz sie dokonal; brak zadan z listy to strata mniejsza niz
            // wyjatek, ktory zatrzymalby pozostale handlery tego zdarzenia.
            logger.LogError(ex, "Lista kontrolna ({Wyzwalacz}) dla {EmployeeId} nie powiodla sie.", wyzwalacz, employeeId);
        }
    }

    private async Task<Guid?> PrzelozonyAsync(Guid tenantId, Guid employeeId, DateTime teraz, CancellationToken ct) =>
        await db.Set<SupervisorRelation>()
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId
                && r.SubordinateEmployeeId == employeeId
                && r.StartDate <= teraz
                && (r.EndDate == null || r.EndDate > teraz))
            .Select(r => (Guid?)r.SupervisorEmployeeId)
            .FirstOrDefaultAsync(ct);
}
