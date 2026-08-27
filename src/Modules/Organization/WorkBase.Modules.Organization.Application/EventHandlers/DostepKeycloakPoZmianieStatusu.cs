using MediatR;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Events;

namespace WorkBase.Modules.Organization.Application.EventHandlers;

/// <summary>
/// Zwolnienie pracownika odbiera mu dostęp; przywrócenie oddaje.
/// </summary>
/// <remarks>
/// <para>
/// <b>To była realna dziura.</b> Istniał <c>ProvisionKeycloakUserOnEmployeeCreated</c>, ale nie
/// istniał handler lustrzany: <c>EmployeeDeactivatedEvent</c> było podnoszone i nikt go nie
/// obsługiwał. Zwolniony pracownik zachowywał konto i mógł się logować dalej — kadry widziały
/// status „Nieaktywny" i miały prawo sądzić, że dostęp zniknął.
/// </para>
/// <para>
/// <b>Wyłączamy konto, nie kasujemy.</b> Skasowane konto zabrałoby ślad, kto co zrobił, i
/// uniemożliwiło powrót przy ponownym zatrudnieniu. Wyłączone konto nie zaloguje się nigdzie,
/// a wszystko poza tym zostaje.
/// </para>
/// <para>
/// <b>Samo wyłączenie nie wystarcza.</b> Konto wyłączone w trakcie sesji nadal ma ważny token
/// dostępu aż do jego wygaśnięcia, więc dodatkowo zamykamy sesje. Bez tego zwolniony pracownik
/// pracowałby dalej do końca ważności tokenu.
/// </para>
/// <para>
/// Robimy to także dla firm zarządzanych z Huba, choć zakładanie kont należy tam do Huba: Hub
/// nie wie, że kogoś zwolniono w kadrach WorkBase, więc gdybyśmy tu odpuścili, nie zrobiłby tego
/// nikt. Wyłączamy konto w realmie firmy, czyli dokładnie tam, gdzie WorkBase decyduje o wejściu.
/// </para>
/// <para>
/// Awaria Keycloaka nie może wycofać zmiany w kadrach — zwolnienie ma zostać zapisane nawet
/// wtedy, gdy odebranie dostępu się nie powiodło. Dlatego log zamiast wyjątku, tak samo jak
/// w handlerze zakładającym konto.
/// </para>
/// </remarks>
public sealed class DostepKeycloakPoZmianieStatusu(
    IEmployeeRepository employeeRepository,
    ITenantRepository tenantRepository,
    IKeycloakAdminService keycloakAdmin,
    ILogger<DostepKeycloakPoZmianieStatusu> logger)
    : INotificationHandler<EmployeeDeactivatedEvent>,
      INotificationHandler<EmployeeActivatedEvent>
{
    public Task Handle(EmployeeDeactivatedEvent notification, CancellationToken cancellationToken) =>
        UstawDostepAsync(notification.EmployeeId, notification.TenantId, wlaczony: false, cancellationToken);

    public Task Handle(EmployeeActivatedEvent notification, CancellationToken cancellationToken) =>
        UstawDostepAsync(notification.EmployeeId, notification.TenantId, wlaczony: true, cancellationToken);

    private async Task UstawDostepAsync(
        Guid employeeId, Guid tenantId, bool wlaczony, CancellationToken cancellationToken)
    {
        try
        {
            var pracownik = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (pracownik?.UserId is null)
            {
                // Pracownik bez konta to normalna sytuacja (praca przy terminalu, brak potrzeby
                // logowania) — nie ma czego odbierac.
                logger.LogInformation(
                    "Pracownik {EmployeeId} nie ma konta w Keycloaku — nie ma czego zmieniac.", employeeId);
                return;
            }

            var firma = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
            var realm = firma?.KeycloakRealmName;

            var ustawione = await keycloakAdmin.SetUserEnabledAsync(
                realm, pracownik.UserId.Value.ToString(), wlaczony, cancellationToken);

            if (!ustawione)
            {
                logger.LogError(
                    "Nie udalo sie ustawic dostepu (wlaczony={Wlaczony}) dla pracownika {EmployeeId}. " +
                    "Konto {UserId} moze nadal dzialac — wymaga recznej weryfikacji.",
                    wlaczony, employeeId, pracownik.UserId);
                return;
            }

            if (!wlaczony)
            {
                // Kolejnosc ma znaczenie: najpierw wylaczamy konto, potem zamykamy sesje.
                // Odwrotna kolejnosc zostawialaby okno na ponowne zalogowanie.
                await keycloakAdmin.LogoutUserSessionsAsync(realm, pracownik.Email, cancellationToken);
            }

            logger.LogInformation(
                "Dostep pracownika {EmployeeId} ustawiony na wlaczony={Wlaczony} (konto {UserId}).",
                employeeId, wlaczony, pracownik.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Zmiana dostepu w Keycloaku nie powiodla sie dla pracownika {EmployeeId}. " +
                "Zmiana statusu w kadrach zostala zapisana.", employeeId);
        }
    }
}
