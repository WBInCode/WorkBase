using NSubstitute;
using WorkBase.Contracts;
using WorkBase.Modules.Leave.Application.Commands;
using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Application.Services;
using WorkBase.Modules.Leave.Domain.Entities;
using WorkBase.Shared.Domain;
using Xunit;

namespace WorkBase.Tests.Unit.Leave;

public class SubmitLeaveRequestHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid EmployeeId = Guid.Parse("00000000-0000-0000-0000-000000000010");

    private readonly ILeaveRequestRepository _requestRepo = Substitute.For<ILeaveRequestRepository>();
    private readonly ILeaveBalanceRepository _balanceRepo = Substitute.For<ILeaveBalanceRepository>();
    private readonly ILeaveTypeRepository _typeRepo = Substitute.For<ILeaveTypeRepository>();
    private readonly ILeaveBalanceCalculator _calculator = Substitute.For<ILeaveBalanceCalculator>();
    private readonly IWorkflowService _workflowService = Substitute.For<IWorkflowService>();
    private readonly SubmitLeaveRequestHandler _handler;

    public SubmitLeaveRequestHandlerTests()
    {
        _handler = new SubmitLeaveRequestHandler(
            _requestRepo, _balanceRepo, _typeRepo, _calculator, _workflowService);
    }

    private static LeaveType CreateLeaveType(bool requiresApproval = true, int? daysPerYear = 26)
    {
        return LeaveType.Create(TenantId, "ANNUAL", "Urlop wypoczynkowy",
            isPaid: true, requiresApproval: requiresApproval, defaultDaysPerYear: daysPerYear);
    }

    private SubmitLeaveRequestCommand CreateCommand(Guid? leaveTypeId = null)
    {
        return new SubmitLeaveRequestCommand(
            EmployeeId, leaveTypeId ?? Guid.NewGuid(),
            new DateTime(2025, 7, 1), new DateTime(2025, 7, 5),
            5, "Wakacje")
        { TenantId = TenantId };
    }

    [Fact]
    public async Task ValidRequest_RequiresApproval_CreatesWorkflowInstance()
    {
        var leaveType = CreateLeaveType(requiresApproval: true);
        var typeId = leaveType.Id;
        var balance = LeaveBalance.Create(TenantId, EmployeeId, typeId, 2025, 26);
        var workflowId = Guid.NewGuid();

        _typeRepo.GetByIdAsync(typeId, Arg.Any<CancellationToken>()).Returns(leaveType);
        _requestRepo.HasOverlappingRequestAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _balanceRepo.GetAsync(TenantId, EmployeeId, typeId, 2025, Arg.Any<CancellationToken>()).Returns(balance);
        _calculator.ValidateBalance(balance, 5).Returns(Result.Success());
        _workflowService.CreateInstanceAsync(
            TenantId, "leave-request-v1", "LeaveRequest", Arg.Any<Guid>(), EmployeeId, Arg.Any<CancellationToken>())
            .Returns(workflowId);

        var command = CreateCommand(typeId);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _workflowService.Received(1).CreateInstanceAsync(
            TenantId, "leave-request-v1", "LeaveRequest",
            Arg.Any<Guid>(), EmployeeId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidRequest_NoApproval_AutoApproves_NoWorkflow()
    {
        var leaveType = CreateLeaveType(requiresApproval: false, daysPerYear: null);
        var typeId = leaveType.Id;

        _typeRepo.GetByIdAsync(typeId, Arg.Any<CancellationToken>()).Returns(leaveType);
        _requestRepo.HasOverlappingRequestAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = CreateCommand(typeId);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _workflowService.DidNotReceive().CreateInstanceAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TypeNotFound_ReturnsFailure()
    {
        _typeRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveType?)null);

        var command = CreateCommand();
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Leave.TypeNotFound", result.Error.Code);
    }

    [Fact]
    public async Task TypeInactive_ReturnsFailure()
    {
        var leaveType = CreateLeaveType();
        leaveType.Deactivate();
        _typeRepo.GetByIdAsync(leaveType.Id, Arg.Any<CancellationToken>()).Returns(leaveType);

        var command = CreateCommand(leaveType.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Leave.TypeInactive", result.Error.Code);
    }

    [Fact]
    public async Task OverlappingRequest_ReturnsConflict()
    {
        var leaveType = CreateLeaveType();
        _typeRepo.GetByIdAsync(leaveType.Id, Arg.Any<CancellationToken>()).Returns(leaveType);
        _requestRepo.HasOverlappingRequestAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var command = CreateCommand(leaveType.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Leave.DateConflict", result.Error.Code);
    }

    [Fact]
    public async Task NoBalance_ReturnsFailure()
    {
        var leaveType = CreateLeaveType();
        _typeRepo.GetByIdAsync(leaveType.Id, Arg.Any<CancellationToken>()).Returns(leaveType);
        _requestRepo.HasOverlappingRequestAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _balanceRepo.GetAsync(TenantId, EmployeeId, leaveType.Id, 2025, Arg.Any<CancellationToken>())
            .Returns((LeaveBalance?)null);

        var command = CreateCommand(leaveType.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Leave.NoBalance", result.Error.Code);
    }

    [Fact]
    public async Task InsufficientBalance_ReturnsFailure()
    {
        var leaveType = CreateLeaveType();
        var balance = LeaveBalance.Create(TenantId, EmployeeId, leaveType.Id, 2025, 3);
        _typeRepo.GetByIdAsync(leaveType.Id, Arg.Any<CancellationToken>()).Returns(leaveType);
        _requestRepo.HasOverlappingRequestAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _balanceRepo.GetAsync(TenantId, EmployeeId, leaveType.Id, 2025, Arg.Any<CancellationToken>())
            .Returns(balance);
        _calculator.ValidateBalance(balance, 5)
            .Returns(Result.Failure(new Error("Leave.InsufficientBalance", "Za mało dni")));

        var command = CreateCommand(leaveType.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Leave.InsufficientBalance", result.Error.Code);
    }

    [Fact]
    public async Task ValidRequest_ReservesPendingDays()
    {
        var leaveType = CreateLeaveType();
        var balance = LeaveBalance.Create(TenantId, EmployeeId, leaveType.Id, 2025, 26);
        _typeRepo.GetByIdAsync(leaveType.Id, Arg.Any<CancellationToken>()).Returns(leaveType);
        _requestRepo.HasOverlappingRequestAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _balanceRepo.GetAsync(TenantId, EmployeeId, leaveType.Id, 2025, Arg.Any<CancellationToken>())
            .Returns(balance);
        _calculator.ValidateBalance(balance, 5).Returns(Result.Success());
        _workflowService.CreateInstanceAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        var command = CreateCommand(leaveType.Id);
        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(5, balance.PendingDays);
        _balanceRepo.Received(1).Update(balance);
    }
}
