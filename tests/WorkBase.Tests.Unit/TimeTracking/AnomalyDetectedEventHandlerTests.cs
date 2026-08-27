using Microsoft.Extensions.Logging;
using NSubstitute;
using WorkBase.Contracts;
using WorkBase.Modules.TimeTracking.Application.EventHandlers;
using WorkBase.Modules.TimeTracking.Domain.Events;
using Xunit;

namespace WorkBase.Tests.Unit.TimeTracking;

public class AnomalyDetectedEventHandlerTests
{
    private readonly ISupervisorLookupService _supervisorLookup = Substitute.For<ISupervisorLookupService>();
    private readonly IOrganizationLookupService _organizationLookup = Substitute.For<IOrganizationLookupService>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly ILogger<AnomalyDetectedEventHandler> _logger = Substitute.For<ILogger<AnomalyDetectedEventHandler>>();
    private readonly AnomalyDetectedEventHandler _handler;

    public AnomalyDetectedEventHandlerTests()
    {
        _handler = new AnomalyDetectedEventHandler(
            _supervisorLookup, _organizationLookup, _notificationService, _logger);
    }

    [Fact]
    public async Task Handle_SupervisorExists_SendsNotification()
    {
        var employeeId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();
        var supervisorUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _supervisorLookup.GetSupervisorEmployeeIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(supervisorId);
        _organizationLookup.GetUserIdByEmployeeIdAsync(supervisorId, Arg.Any<CancellationToken>())
            .Returns(supervisorUserId);

        var evt = new AnomalyDetectedEvent(Guid.NewGuid(), tenantId, employeeId, "MissingClockOut", new DateOnly(2026, 4, 16));

        await _handler.Handle(evt, CancellationToken.None);

        await _supervisorLookup.Received(1).GetSupervisorEmployeeIdAsync(employeeId, Arg.Any<CancellationToken>());
        await _notificationService.Received(1).SendFromTemplateAsync(
            tenantId, supervisorUserId,
            "anomaly_detected", Arg.Any<IReadOnlyDictionary<string, string?>>(),
            Arg.Any<string>(), Arg.Any<string>(),
            "anomaly_detected",
            "anomaly", evt.AnomalyId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UzywaNazwiskaIPolskiegoOpisuRodzaju()
    {
        var employeeId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();
        var supervisorUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _supervisorLookup.GetSupervisorEmployeeIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(supervisorId);
        _organizationLookup.GetUserIdByEmployeeIdAsync(supervisorId, Arg.Any<CancellationToken>())
            .Returns(supervisorUserId);
        _organizationLookup.GetEmployeeFullNameAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns("Ewa Adamczyk");

        var evt = new AnomalyDetectedEvent(Guid.NewGuid(), tenantId, employeeId, "MissingClockIn", new DateOnly(2026, 4, 16));

        await _handler.Handle(evt, CancellationToken.None);

        // Tresc idzie przez szablon firmy, ale wartosci awaryjne to dawne teksty z kodu —
        // sprawdzamy wlasnie je, bo brak szablonu nie moze zmienic tego, co widzi uzytkownik.
        // Dodatkowo pilnujemy zmiennych: to one trafiaja do szablonu, gdy firma go ustawi.
        await _notificationService.Received(1).SendFromTemplateAsync(
            tenantId, supervisorUserId,
            "anomaly_detected",
            Arg.Is<IReadOnlyDictionary<string, string?>>(z =>
                z["rodzaj"] == "brak wejścia"
                && z["pracownik"] == "Ewa Adamczyk"
                && z["data"] == "16.04.2026"),
            "Anomalia: brak wejścia",
            "Ewa Adamczyk: brak wejścia w dniu 16.04.2026.",
            "anomaly_detected", "anomaly", evt.AnomalyId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SupervisorWithoutAccount_SkipsNotification()
    {
        var employeeId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();
        _supervisorLookup.GetSupervisorEmployeeIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(supervisorId);
        _organizationLookup.GetUserIdByEmployeeIdAsync(supervisorId, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var evt = new AnomalyDetectedEvent(Guid.NewGuid(), Guid.NewGuid(), employeeId, "MissingClockOut", new DateOnly(2026, 4, 16));

        await _handler.Handle(evt, CancellationToken.None);

        await _notificationService.DidNotReceiveWithAnyArgs().SendFromTemplateAsync(
            default, default, default!, default!, default!, default!, default!, default, default, default);
    }

    [Fact]
    public async Task Handle_NoSupervisor_SkipsNotification()
    {
        var employeeId = Guid.NewGuid();
        _supervisorLookup.GetSupervisorEmployeeIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var evt = new AnomalyDetectedEvent(Guid.NewGuid(), Guid.NewGuid(), employeeId, "LateArrival", new DateOnly(2026, 4, 16));

        await _handler.Handle(evt, CancellationToken.None);

        await _supervisorLookup.Received(1).GetSupervisorEmployeeIdAsync(employeeId, Arg.Any<CancellationToken>());
        await _notificationService.DidNotReceiveWithAnyArgs().SendFromTemplateAsync(
            default, default, default!, default!, default!, default!, default!, default, default, default);
    }
}
