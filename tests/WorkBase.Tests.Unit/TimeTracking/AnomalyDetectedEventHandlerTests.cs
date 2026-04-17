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
    private readonly ILogger<AnomalyDetectedEventHandler> _logger = Substitute.For<ILogger<AnomalyDetectedEventHandler>>();
    private readonly AnomalyDetectedEventHandler _handler;

    public AnomalyDetectedEventHandlerTests()
    {
        _handler = new AnomalyDetectedEventHandler(_supervisorLookup, _logger);
    }

    [Fact]
    public async Task Handle_SupervisorExists_LogsNotification()
    {
        var employeeId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();
        _supervisorLookup.GetSupervisorEmployeeIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(supervisorId);

        var evt = new AnomalyDetectedEvent(Guid.NewGuid(), Guid.NewGuid(), employeeId, "MissingClockOut", new DateOnly(2026, 4, 16));

        await _handler.Handle(evt, CancellationToken.None);

        await _supervisorLookup.Received(1).GetSupervisorEmployeeIdAsync(employeeId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoSupervisor_SkipsNotification()
    {
        var employeeId = Guid.NewGuid();
        _supervisorLookup.GetSupervisorEmployeeIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var evt = new AnomalyDetectedEvent(Guid.NewGuid(), Guid.NewGuid(), employeeId, "LateArrival", new DateOnly(2026, 4, 16));

        await _handler.Handle(evt, CancellationToken.None);

        // Should not throw, just log and skip
        await _supervisorLookup.Received(1).GetSupervisorEmployeeIdAsync(employeeId, Arg.Any<CancellationToken>());
    }
}
