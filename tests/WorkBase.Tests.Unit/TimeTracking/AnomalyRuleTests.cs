using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Rules;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Unit.TimeTracking;

public class AnomalyRuleTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid EmployeeId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly DateOnly TestDate = new(2026, 4, 16);

    private static AnomalyRuleContext CreateContext(
        List<TimeEntry>? entries = null,
        Schedule? schedule = null,
        TimeSheet? timeSheet = null,
        AnomalyDetectionSettings? settings = null)
    {
        return new AnomalyRuleContext
        {
            TenantId = TenantId,
            EmployeeId = EmployeeId,
            Date = TestDate,
            Entries = entries ?? [],
            Schedule = schedule,
            TimeSheet = timeSheet,
            Settings = settings ?? new AnomalyDetectionSettings(),
            AlreadyDetected = (_, _) => Task.FromResult(false),
        };
    }

    // --- MissingClockOutRule ---

    [Fact]
    public async Task MissingClockOut_LastEntryIsClockIn_DetectsAnomaly()
    {
        var rule = new MissingClockOutRule();
        var entries = new List<TimeEntry>
        {
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(8, 0)), TimeEntryType.ClockIn),
        };

        var result = await rule.EvaluateAsync(CreateContext(entries: entries));

        Assert.Single(result);
        Assert.Equal(AnomalyType.MissingClockOut, result[0].Type);
    }

    [Fact]
    public async Task MissingClockOut_LastEntryIsClockOut_NoAnomaly()
    {
        var rule = new MissingClockOutRule();
        var entries = new List<TimeEntry>
        {
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(8, 0)), TimeEntryType.ClockIn),
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(16, 0)), TimeEntryType.ClockOut),
        };

        var result = await rule.EvaluateAsync(CreateContext(entries: entries));

        Assert.Empty(result);
    }

    [Fact]
    public async Task MissingClockOut_DisabledInSettings_NoAnomaly()
    {
        var rule = new MissingClockOutRule();
        var entries = new List<TimeEntry>
        {
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(8, 0)), TimeEntryType.ClockIn),
        };
        var settings = new AnomalyDetectionSettings { DetectMissingClockOut = false };

        var result = await rule.EvaluateAsync(CreateContext(entries: entries, settings: settings));

        Assert.Empty(result);
    }

    [Fact]
    public async Task MissingClockOut_AlreadyDetected_NoAnomaly()
    {
        var rule = new MissingClockOutRule();
        var entries = new List<TimeEntry>
        {
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(8, 0)), TimeEntryType.ClockIn),
        };
        var ctx = CreateContext(entries: entries);
        // Override AlreadyDetected to return true
        ctx = new AnomalyRuleContext
        {
            TenantId = ctx.TenantId, EmployeeId = ctx.EmployeeId, Date = ctx.Date,
            Entries = ctx.Entries, Schedule = ctx.Schedule, TimeSheet = ctx.TimeSheet,
            Settings = ctx.Settings,
            AlreadyDetected = (_, _) => Task.FromResult(true),
        };

        var result = await rule.EvaluateAsync(ctx);

        Assert.Empty(result);
    }

    // --- MissingClockInRule ---

    [Fact]
    public async Task MissingClockIn_ScheduleExistsNoEntries_DetectsAnomaly()
    {
        var rule = new MissingClockInRule();
        var schedule = Schedule.Create(TenantId, EmployeeId, TestDate, new TimeOnly(8, 0), new TimeOnly(16, 0));

        var result = await rule.EvaluateAsync(CreateContext(schedule: schedule));

        Assert.Single(result);
        Assert.Equal(AnomalyType.MissingClockIn, result[0].Type);
    }

    [Fact]
    public async Task MissingClockIn_NoSchedule_NoAnomaly()
    {
        var rule = new MissingClockInRule();

        var result = await rule.EvaluateAsync(CreateContext());

        Assert.Empty(result);
    }

    // --- LateArrivalRule ---

    [Fact]
    public async Task LateArrival_ClockInAfterThreshold_DetectsAnomaly()
    {
        var rule = new LateArrivalRule();
        var schedule = Schedule.Create(TenantId, EmployeeId, TestDate, new TimeOnly(8, 0), new TimeOnly(16, 0));
        var entries = new List<TimeEntry>
        {
            // 30 min late (threshold is 15 min by default)
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(8, 30)), TimeEntryType.ClockIn),
        };

        var result = await rule.EvaluateAsync(CreateContext(entries: entries, schedule: schedule));

        Assert.Single(result);
        Assert.Equal(AnomalyType.LateArrival, result[0].Type);
        Assert.Contains("30", result[0].Description!);
    }

    [Fact]
    public async Task LateArrival_ClockInWithinThreshold_NoAnomaly()
    {
        var rule = new LateArrivalRule();
        var schedule = Schedule.Create(TenantId, EmployeeId, TestDate, new TimeOnly(8, 0), new TimeOnly(16, 0));
        var entries = new List<TimeEntry>
        {
            // 10 min late (within 15 min threshold)
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(8, 10)), TimeEntryType.ClockIn),
        };

        var result = await rule.EvaluateAsync(CreateContext(entries: entries, schedule: schedule));

        Assert.Empty(result);
    }

    [Fact]
    public async Task LateArrival_CustomThreshold_Configurable()
    {
        var rule = new LateArrivalRule();
        var schedule = Schedule.Create(TenantId, EmployeeId, TestDate, new TimeOnly(8, 0), new TimeOnly(16, 0));
        var entries = new List<TimeEntry>
        {
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(8, 10)), TimeEntryType.ClockIn),
        };
        var settings = new AnomalyDetectionSettings { LateArrivalThreshold = TimeSpan.FromMinutes(5) };

        var result = await rule.EvaluateAsync(CreateContext(entries: entries, schedule: schedule, settings: settings));

        Assert.Single(result); // 10 min late > 5 min threshold
    }

    // --- DoubleClockInRule ---

    [Fact]
    public async Task DoubleClockIn_ConsecutiveClockIns_DetectsAnomaly()
    {
        var rule = new DoubleClockInRule();
        var entries = new List<TimeEntry>
        {
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(8, 0)), TimeEntryType.ClockIn),
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(8, 5)), TimeEntryType.ClockIn),
        };

        var result = await rule.EvaluateAsync(CreateContext(entries: entries));

        Assert.Single(result);
        Assert.Equal(AnomalyType.DoubleClockIn, result[0].Type);
    }

    [Fact]
    public async Task DoubleClockIn_NormalSequence_NoAnomaly()
    {
        var rule = new DoubleClockInRule();
        var entries = new List<TimeEntry>
        {
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(8, 0)), TimeEntryType.ClockIn),
            TimeEntry.Create(TenantId, EmployeeId, TestDate.ToDateTime(new TimeOnly(16, 0)), TimeEntryType.ClockOut),
        };

        var result = await rule.EvaluateAsync(CreateContext(entries: entries));

        Assert.Empty(result);
    }

    // --- OverlongShiftRule ---

    [Fact]
    public async Task OverlongShift_ExceedsThreshold_DetectsAnomaly()
    {
        var rule = new OverlongShiftRule();
        var timeSheet = TimeSheet.Create(TenantId, EmployeeId, TestDate);
        timeSheet.Recalculate(TimeSpan.FromHours(14), TimeSpan.FromHours(1)); // 14h > 12h threshold

        var result = await rule.EvaluateAsync(CreateContext(timeSheet: timeSheet));

        Assert.Single(result);
        Assert.Equal(AnomalyType.ExcessiveShift, result[0].Type);
    }

    [Fact]
    public async Task OverlongShift_WithinThreshold_NoAnomaly()
    {
        var rule = new OverlongShiftRule();
        var timeSheet = TimeSheet.Create(TenantId, EmployeeId, TestDate);
        timeSheet.Recalculate(TimeSpan.FromHours(8), TimeSpan.FromMinutes(30));

        var result = await rule.EvaluateAsync(CreateContext(timeSheet: timeSheet));

        Assert.Empty(result);
    }

    [Fact]
    public async Task OverlongShift_CustomThreshold_Configurable()
    {
        var rule = new OverlongShiftRule();
        var timeSheet = TimeSheet.Create(TenantId, EmployeeId, TestDate);
        timeSheet.Recalculate(TimeSpan.FromHours(9), TimeSpan.FromMinutes(30));
        var settings = new AnomalyDetectionSettings { ExcessiveShiftThreshold = TimeSpan.FromHours(8) };

        var result = await rule.EvaluateAsync(CreateContext(timeSheet: timeSheet, settings: settings));

        Assert.Single(result); // 9h > 8h threshold
    }

    [Fact]
    public async Task OverlongShift_NoTimeSheet_NoAnomaly()
    {
        var rule = new OverlongShiftRule();

        var result = await rule.EvaluateAsync(CreateContext());

        Assert.Empty(result);
    }
}
