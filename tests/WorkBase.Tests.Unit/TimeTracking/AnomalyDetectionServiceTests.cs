using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Rules;
using WorkBase.Modules.TimeTracking.Application.Services;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Unit.TimeTracking;

public class AnomalyDetectionServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid EmployeeId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly DateOnly TestDate = new(2026, 4, 16);

    private readonly ITimeEntryRepository _entryRepo = Substitute.For<ITimeEntryRepository>();
    private readonly IScheduleRepository _scheduleRepo = Substitute.For<IScheduleRepository>();
    private readonly ITimeAnomalyRepository _anomalyRepo = Substitute.For<ITimeAnomalyRepository>();
    private readonly ITimeSheetRepository _timesheetRepo = Substitute.For<ITimeSheetRepository>();
    private readonly ILogger<AnomalyDetectionService> _logger = Substitute.For<ILogger<AnomalyDetectionService>>();

    private AnomalyDetectionService CreateService(params IAnomalyRule[] rules)
    {
        return new AnomalyDetectionService(
            _entryRepo, _scheduleRepo, _anomalyRepo, _timesheetRepo, rules, _logger);
    }

    [Fact]
    public async Task DetectAnomalies_ExecutesAllRules()
    {
        var entries = new List<TimeEntry>
        {
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(8, 0)), TimeEntryType.ClockIn),
        };

        _entryRepo.GetEntriesForDateAsync(TenantId, EmployeeId, TestDate, Arg.Any<CancellationToken>())
            .Returns(entries);
        _scheduleRepo.GetByDateAsync(TenantId, EmployeeId, TestDate, Arg.Any<CancellationToken>())
            .Returns((Schedule?)null);
        _timesheetRepo.GetByDateAsync(TenantId, EmployeeId, TestDate, Arg.Any<CancellationToken>())
            .Returns((TimeSheet?)null);
        _anomalyRepo.ExistsAsync(TenantId, EmployeeId, TestDate, Arg.Any<AnomalyType>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var service = CreateService(
            new MissingClockOutRule(),
            new MissingClockInRule(),
            new LateArrivalRule(),
            new DoubleClockInRule(),
            new OverlongShiftRule());

        var result = await service.DetectAnomaliesForDateAsync(
            TenantId, EmployeeId, TestDate, new AnomalyDetectionSettings());

        // Only MissingClockOut should fire (last entry is ClockIn, no ClockOut)
        Assert.Single(result);
        Assert.Equal(AnomalyType.MissingClockOut, result[0].Type);

        // Anomaly should be persisted
        await _anomalyRepo.Received(1).AddAsync(Arg.Any<TimeAnomaly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DetectAnomalies_MultipleRulesFire()
    {
        var schedule = Schedule.Create(TenantId, EmployeeId, TestDate, new TimeOnly(8, 0), new TimeOnly(16, 0));
        var entries = new List<TimeEntry>
        {
            // 30 min late + missing clock-out
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(8, 30)), TimeEntryType.ClockIn),
        };

        _entryRepo.GetEntriesForDateAsync(TenantId, EmployeeId, TestDate, Arg.Any<CancellationToken>())
            .Returns(entries);
        _scheduleRepo.GetByDateAsync(TenantId, EmployeeId, TestDate, Arg.Any<CancellationToken>())
            .Returns(schedule);
        _timesheetRepo.GetByDateAsync(TenantId, EmployeeId, TestDate, Arg.Any<CancellationToken>())
            .Returns((TimeSheet?)null);
        _anomalyRepo.ExistsAsync(TenantId, EmployeeId, TestDate, Arg.Any<AnomalyType>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var service = CreateService(
            new MissingClockOutRule(),
            new LateArrivalRule());

        var result = await service.DetectAnomaliesForDateAsync(
            TenantId, EmployeeId, TestDate, new AnomalyDetectionSettings());

        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Type == AnomalyType.MissingClockOut);
        Assert.Contains(result, a => a.Type == AnomalyType.LateArrival);
    }

    [Fact]
    public async Task DetectAnomalies_RuleException_ContinuesOtherRules()
    {
        var faultyRule = Substitute.For<IAnomalyRule>();
        faultyRule.EvaluateAsync(Arg.Any<AnomalyRuleContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Boom"));

        var entries = new List<TimeEntry>
        {
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(8, 0)), TimeEntryType.ClockIn),
        };

        _entryRepo.GetEntriesForDateAsync(TenantId, EmployeeId, TestDate, Arg.Any<CancellationToken>())
            .Returns(entries);
        _scheduleRepo.GetByDateAsync(TenantId, EmployeeId, TestDate, Arg.Any<CancellationToken>())
            .Returns((Schedule?)null);
        _timesheetRepo.GetByDateAsync(TenantId, EmployeeId, TestDate, Arg.Any<CancellationToken>())
            .Returns((TimeSheet?)null);
        _anomalyRepo.ExistsAsync(TenantId, EmployeeId, TestDate, Arg.Any<AnomalyType>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var service = CreateService(faultyRule, new MissingClockOutRule());

        var result = await service.DetectAnomaliesForDateAsync(
            TenantId, EmployeeId, TestDate, new AnomalyDetectionSettings());

        // Faulty rule should not prevent MissingClockOut from running
        Assert.Single(result);
        Assert.Equal(AnomalyType.MissingClockOut, result[0].Type);
    }

    [Fact]
    public async Task DetectAnomalies_NoRules_EmptyResult()
    {
        _entryRepo.GetEntriesForDateAsync(TenantId, EmployeeId, TestDate, Arg.Any<CancellationToken>())
            .Returns(new List<TimeEntry>());
        _scheduleRepo.GetByDateAsync(TenantId, EmployeeId, TestDate, Arg.Any<CancellationToken>())
            .Returns((Schedule?)null);
        _timesheetRepo.GetByDateAsync(TenantId, EmployeeId, TestDate, Arg.Any<CancellationToken>())
            .Returns((TimeSheet?)null);

        var service = CreateService(); // no rules

        var result = await service.DetectAnomaliesForDateAsync(
            TenantId, EmployeeId, TestDate, new AnomalyDetectionSettings());

        Assert.Empty(result);
    }
}
