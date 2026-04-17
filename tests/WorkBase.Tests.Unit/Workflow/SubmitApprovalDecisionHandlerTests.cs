using NSubstitute;
using WorkBase.Modules.Workflow.Application;
using WorkBase.Modules.Workflow.Application.Commands;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Domain.Entities;
using WorkBase.Shared.Domain;
using Xunit;

namespace WorkBase.Tests.Unit.Workflow;

public class SubmitApprovalDecisionHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid ApproverId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    private static readonly Guid RequesterId = Guid.Parse("00000000-0000-0000-0000-000000000020");

    private readonly IApprovalRequestRepository _requestRepo = Substitute.For<IApprovalRequestRepository>();
    private readonly IApprovalDecisionRepository _decisionRepo = Substitute.For<IApprovalDecisionRepository>();
    private readonly IWorkflowEngine _workflowEngine = Substitute.For<IWorkflowEngine>();
    private readonly SubmitApprovalDecisionHandler _handler;

    public SubmitApprovalDecisionHandlerTests()
    {
        _handler = new SubmitApprovalDecisionHandler(_requestRepo, _decisionRepo, _workflowEngine);
    }

    private ApprovalRequest CreatePendingRequest(Guid? id = null)
    {
        var request = ApprovalRequest.Create(
            TenantId,
            stepId: Guid.NewGuid(),
            instanceId: Guid.NewGuid(),
            requesterId: RequesterId,
            approverId: ApproverId);
        if (id.HasValue)
        {
            typeof(ApprovalRequest).BaseType!.BaseType!
                .GetProperty("Id")!.SetValue(request, id.Value);
        }
        // Clear domain events from creation
        request.ClearDomainEvents();
        return request;
    }

    [Theory]
    [InlineData("approve")]
    [InlineData("reject")]
    [InlineData("return")]
    public async Task ValidDecision_ByAssignedApprover_Succeeds(string decision)
    {
        var requestId = Guid.NewGuid();
        var approvalRequest = CreatePendingRequest(requestId);

        _requestRepo.GetByIdAsync(requestId, Arg.Any<CancellationToken>()).Returns(approvalRequest);
        _workflowEngine.AdvanceStepAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(decision == "approve" ? "approved" : decision == "reject" ? "rejected" : "submitted"));

        var command = new SubmitApprovalDecisionCommand(requestId, decision, ApproverId, "Komentarz")
        {
            TenantId = TenantId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _decisionRepo.Received(1).AddAsync(Arg.Is<ApprovalDecision>(d =>
            d.Decision == decision &&
            d.DecidedBy == ApproverId), Arg.Any<CancellationToken>());
        _requestRepo.Received(1).Update(approvalRequest);
    }

    [Fact]
    public async Task Approve_ChangesRequestStatusToApproved()
    {
        var requestId = Guid.NewGuid();
        var approvalRequest = CreatePendingRequest(requestId);

        _requestRepo.GetByIdAsync(requestId, Arg.Any<CancellationToken>()).Returns(approvalRequest);
        _workflowEngine.AdvanceStepAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("approved"));

        var command = new SubmitApprovalDecisionCommand(requestId, "approve", ApproverId) { TenantId = TenantId };
        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Approved", approvalRequest.Status);
    }

    [Fact]
    public async Task Reject_ChangesRequestStatusToRejected()
    {
        var requestId = Guid.NewGuid();
        var approvalRequest = CreatePendingRequest(requestId);

        _requestRepo.GetByIdAsync(requestId, Arg.Any<CancellationToken>()).Returns(approvalRequest);
        _workflowEngine.AdvanceStepAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("rejected"));

        var command = new SubmitApprovalDecisionCommand(requestId, "reject", ApproverId) { TenantId = TenantId };
        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Rejected", approvalRequest.Status);
    }

    [Fact]
    public async Task Return_ChangesRequestStatusToReturned()
    {
        var requestId = Guid.NewGuid();
        var approvalRequest = CreatePendingRequest(requestId);

        _requestRepo.GetByIdAsync(requestId, Arg.Any<CancellationToken>()).Returns(approvalRequest);
        _workflowEngine.AdvanceStepAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("submitted"));

        var command = new SubmitApprovalDecisionCommand(requestId, "return", ApproverId) { TenantId = TenantId };
        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Returned", approvalRequest.Status);
    }

    [Fact]
    public async Task InvalidDecision_ReturnsFailure()
    {
        var command = new SubmitApprovalDecisionCommand(Guid.NewGuid(), "invalid", ApproverId) { TenantId = TenantId };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Approval.InvalidDecision", result.Error.Code);
    }

    [Fact]
    public async Task RequestNotFound_ReturnsNotFound()
    {
        var requestId = Guid.NewGuid();
        _requestRepo.GetByIdAsync(requestId, Arg.Any<CancellationToken>()).Returns((ApprovalRequest?)null);

        var command = new SubmitApprovalDecisionCommand(requestId, "approve", ApproverId) { TenantId = TenantId };
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Approval.RequestNotFound", result.Error.Code);
    }

    [Fact]
    public async Task AlreadyDecided_ReturnsFailure()
    {
        var requestId = Guid.NewGuid();
        var approvalRequest = CreatePendingRequest(requestId);
        approvalRequest.Approve(); // already decided

        _requestRepo.GetByIdAsync(requestId, Arg.Any<CancellationToken>()).Returns(approvalRequest);

        var command = new SubmitApprovalDecisionCommand(requestId, "approve", ApproverId) { TenantId = TenantId };
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Approval.AlreadyDecided", result.Error.Code);
    }

    [Fact]
    public async Task WrongApprover_ReturnsForbidden()
    {
        var requestId = Guid.NewGuid();
        var approvalRequest = CreatePendingRequest(requestId);
        var wrongApprover = Guid.NewGuid();

        _requestRepo.GetByIdAsync(requestId, Arg.Any<CancellationToken>()).Returns(approvalRequest);

        var command = new SubmitApprovalDecisionCommand(requestId, "approve", wrongApprover) { TenantId = TenantId };
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Approval.NotAuthorized", result.Error.Code);
    }

    [Fact]
    public async Task Approve_AdvancesWorkflowWithApprovedOutcome()
    {
        var requestId = Guid.NewGuid();
        var approvalRequest = CreatePendingRequest(requestId);

        _requestRepo.GetByIdAsync(requestId, Arg.Any<CancellationToken>()).Returns(approvalRequest);
        _workflowEngine.AdvanceStepAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("approved"));

        var command = new SubmitApprovalDecisionCommand(requestId, "approve", ApproverId, "OK") { TenantId = TenantId };
        await _handler.Handle(command, CancellationToken.None);

        await _workflowEngine.Received(1).AdvanceStepAsync(
            approvalRequest.InstanceId,
            "approved",
            ApproverId.ToString(),
            "OK",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WorkflowAdvanceFails_ReturnsFailure()
    {
        var requestId = Guid.NewGuid();
        var approvalRequest = CreatePendingRequest(requestId);

        _requestRepo.GetByIdAsync(requestId, Arg.Any<CancellationToken>()).Returns(approvalRequest);
        _workflowEngine.AdvanceStepAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(new Error("Workflow.Error", "Coś poszło nie tak")));

        var command = new SubmitApprovalDecisionCommand(requestId, "approve", ApproverId) { TenantId = TenantId };
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}
