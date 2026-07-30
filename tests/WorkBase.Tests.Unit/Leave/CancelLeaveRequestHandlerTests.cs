using NSubstitute;
using WorkBase.Modules.Leave.Application.Commands;
using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Unit.Leave;

public class CancelLeaveRequestHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid OwnerEmployeeId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    private static readonly Guid OtherEmployeeId = Guid.Parse("00000000-0000-0000-0000-000000000011");

    private readonly ILeaveRequestRepository _requestRepo = Substitute.For<ILeaveRequestRepository>();
    private readonly ILeaveBalanceRepository _balanceRepo = Substitute.For<ILeaveBalanceRepository>();
    private readonly CancelLeaveRequestHandler _handler;

    public CancelLeaveRequestHandlerTests()
    {
        _handler = new CancelLeaveRequestHandler(_requestRepo, _balanceRepo);
    }

    private LeaveRequest ArrangeRequest()
    {
        var leaveRequest = LeaveRequest.Create(
            TenantId, OwnerEmployeeId, Guid.NewGuid(),
            new DateTime(2025, 7, 1), new DateTime(2025, 7, 5), 5, "Wakacje");
        _requestRepo.GetByIdAsync(leaveRequest.Id, Arg.Any<CancellationToken>()).Returns(leaveRequest);
        return leaveRequest;
    }

    [Fact]
    public async Task ForeignRequest_WithoutManagePermission_IsRejected()
    {
        var leaveRequest = ArrangeRequest();
        var command = new CancelLeaveRequestCommand(leaveRequest.Id, OtherEmployeeId) { TenantId = TenantId };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("LeaveRequest.NotOwner", result.Error.Code);
        _requestRepo.DidNotReceive().Update(Arg.Any<LeaveRequest>());
    }

    [Fact]
    public async Task OwnRequest_IsCancelled()
    {
        var leaveRequest = ArrangeRequest();
        var command = new CancelLeaveRequestCommand(leaveRequest.Id, OwnerEmployeeId) { TenantId = TenantId };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _requestRepo.Received(1).Update(leaveRequest);
    }

    [Fact]
    public async Task ManagePermission_CancelsForeignRequest()
    {
        var leaveRequest = ArrangeRequest();
        var command = new CancelLeaveRequestCommand(leaveRequest.Id) { TenantId = TenantId };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _requestRepo.Received(1).Update(leaveRequest);
    }
}
