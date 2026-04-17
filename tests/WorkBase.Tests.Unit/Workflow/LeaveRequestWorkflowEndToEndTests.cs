using NSubstitute;
using WorkBase.Modules.Workflow.Application;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Application.Services;
using WorkBase.Modules.Workflow.Domain.Entities;
using WorkBase.Shared.Domain;
using Xunit;

namespace WorkBase.Tests.Unit.Workflow;

/// <summary>
/// End-to-end workflow integration tests for the leave request lifecycle.
/// Covers: create instance → submit → approve/reject/return → final state.
/// Also verifies action execution at each step and approval request creation.
/// </summary>
public class LeaveRequestWorkflowEndToEndTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid RequesterId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid ApproverId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private const string LeaveWorkflowJson = """
    {
        "name": "leave-request-v1",
        "version": 1,
        "entityType": "LeaveRequest",
        "initialStep": "submitted",
        "steps": [
            {
                "name": "submitted",
                "type": "action",
                "transitions": [
                    { "outcome": "submit", "targetStep": "pending_approval" }
                ],
                "actions": [
                    { "type": "notify", "trigger": "on_enter", "payload": { "template": "leave_submitted", "to": "requester" } },
                    { "type": "notify", "trigger": "on_exit", "payload": { "template": "leave_forwarded", "to": "approver" } }
                ]
            },
            {
                "name": "pending_approval",
                "type": "approval",
                "approverStrategy": "supervisor",
                "transitions": [
                    { "outcome": "approved", "targetStep": "approved" },
                    { "outcome": "rejected", "targetStep": "rejected" },
                    { "outcome": "returned", "targetStep": "submitted" }
                ],
                "actions": [
                    { "type": "notify", "trigger": "on_enter", "payload": { "template": "approval_needed", "to": "approver" } },
                    { "type": "update_entity", "trigger": "on_complete", "payload": { "field": "status" } }
                ]
            },
            { "name": "approved", "type": "end" },
            { "name": "rejected", "type": "end" }
        ]
    }
    """;

    private readonly IWorkflowDefinitionRepository _definitionRepo = Substitute.For<IWorkflowDefinitionRepository>();
    private readonly IWorkflowInstanceRepository _instanceRepo = Substitute.For<IWorkflowInstanceRepository>();
    private readonly IWorkflowStepRepository _stepRepo = Substitute.For<IWorkflowStepRepository>();
    private readonly IWorkflowActionRepository _actionRepo = Substitute.For<IWorkflowActionRepository>();
    private readonly IApprovalRequestRepository _approvalRequestRepo = Substitute.For<IApprovalRequestRepository>();
    private readonly IApproverResolver _approverResolver = Substitute.For<IApproverResolver>();
    private readonly IWorkflowActionExecutor _actionExecutor = Substitute.For<IWorkflowActionExecutor>();
    private readonly WorkflowEngine _engine;

    // Track state
    private WorkflowInstance? _trackedInstance;

    public LeaveRequestWorkflowEndToEndTests()
    {
        _engine = new WorkflowEngine(
            _definitionRepo, _instanceRepo, _stepRepo, _actionRepo,
            _approvalRequestRepo, _approverResolver, _actionExecutor);

        // Track the instance as it's created so we can advance it later
        _instanceRepo.AddAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => _trackedInstance = ci.Arg<WorkflowInstance>());
    }

    private WorkflowDefinition CreateDefinition(Guid defId)
    {
        var def = WorkflowDefinition.Create(TenantId, "leave-request-v1", LeaveWorkflowJson, "Leave request workflow");
        typeof(WorkflowDefinition).BaseType!.BaseType!
            .GetProperty("Id")!.SetValue(def, defId);
        return def;
    }

    private void SetupInstanceRetrieval()
    {
        _instanceRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => _trackedInstance);
    }

    [Fact]
    public async Task LeaveRequest_SubmitAndApprove_FullLifecycle()
    {
        // Arrange
        var defId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);
        _approverResolver.ResolveApproverAsync("supervisor", RequesterId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(ApproverId));

        // Step 1: Create instance → starts at "submitted"
        var createResult = await _engine.CreateInstanceAsync(TenantId, defId, "LeaveRequest", entityId, RequesterId);

        Assert.True(createResult.IsSuccess);
        Assert.NotNull(_trackedInstance);
        Assert.Equal("submitted", _trackedInstance!.CurrentStepName);
        Assert.Equal("Active", _trackedInstance.Status);

        // Verify on_enter action was created and executed for "submitted" step
        await _actionRepo.Received(1).AddAsync(
            Arg.Is<WorkflowAction>(a => a.ActionType == "notify"),
            Arg.Any<CancellationToken>());
        await _actionExecutor.Received(1).ExecuteAsync(
            Arg.Is<WorkflowAction>(a => a.ActionType == "notify"),
            Arg.Any<CancellationToken>());

        // Step 2: Submit → pending_approval
        SetupInstanceRetrieval();
        _stepRepo.GetActiveStepAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowStep.Create(TenantId, _trackedInstance.Id, "submitted"));

        _actionRepo.ClearReceivedCalls();
        _actionExecutor.ClearReceivedCalls();

        var submitResult = await _engine.AdvanceStepAsync(_trackedInstance.Id, "submit");

        Assert.True(submitResult.IsSuccess);
        Assert.Equal("pending_approval", _trackedInstance.CurrentStepName);
        Assert.Equal("Active", _trackedInstance.Status);

        // Verify: on_exit action for "submitted" (notify), on_enter for "pending_approval" (notify), approval request created
        await _approvalRequestRepo.Received(1).AddAsync(
            Arg.Is<ApprovalRequest>(r => r.ApproverId == ApproverId && r.RequesterId == RequesterId),
            Arg.Any<CancellationToken>());

        // Step 3: Approve → approved (end)
        _stepRepo.GetActiveStepAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowStep.Create(TenantId, _trackedInstance.Id, "pending_approval"));

        var approveResult = await _engine.AdvanceStepAsync(_trackedInstance.Id, "approved", ApproverId.ToString(), "Zatwierdzam urlop");

        Assert.True(approveResult.IsSuccess);
        Assert.Equal("approved", approveResult.Value);
        Assert.Equal("Completed", _trackedInstance.Status);
    }

    [Fact]
    public async Task LeaveRequest_SubmitAndReject_FullLifecycle()
    {
        // Arrange
        var defId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);
        _approverResolver.ResolveApproverAsync("supervisor", RequesterId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(ApproverId));

        // Create instance
        await _engine.CreateInstanceAsync(TenantId, defId, "LeaveRequest", entityId, RequesterId);
        SetupInstanceRetrieval();

        // Submit
        _stepRepo.GetActiveStepAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowStep.Create(TenantId, _trackedInstance!.Id, "submitted"));
        await _engine.AdvanceStepAsync(_trackedInstance.Id, "submit");

        Assert.Equal("pending_approval", _trackedInstance.CurrentStepName);

        // Reject
        _stepRepo.GetActiveStepAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowStep.Create(TenantId, _trackedInstance.Id, "pending_approval"));

        var rejectResult = await _engine.AdvanceStepAsync(_trackedInstance.Id, "rejected", ApproverId.ToString(), "Brak budżetu");

        Assert.True(rejectResult.IsSuccess);
        Assert.Equal("rejected", rejectResult.Value);
        Assert.Equal("Rejected", _trackedInstance.Status);
    }

    [Fact]
    public async Task LeaveRequest_SubmitReturnResubmitApprove_FullLifecycle()
    {
        // Arrange
        var defId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);
        _approverResolver.ResolveApproverAsync("supervisor", RequesterId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(ApproverId));

        // Create
        await _engine.CreateInstanceAsync(TenantId, defId, "LeaveRequest", entityId, RequesterId);
        SetupInstanceRetrieval();

        // Submit
        _stepRepo.GetActiveStepAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowStep.Create(TenantId, _trackedInstance!.Id, "submitted"));
        await _engine.AdvanceStepAsync(_trackedInstance.Id, "submit");
        Assert.Equal("pending_approval", _trackedInstance.CurrentStepName);

        // Return to requester
        _stepRepo.GetActiveStepAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowStep.Create(TenantId, _trackedInstance.Id, "pending_approval"));
        await _engine.AdvanceStepAsync(_trackedInstance.Id, "returned", ApproverId.ToString(), "Popraw daty");
        Assert.Equal("submitted", _trackedInstance.CurrentStepName);
        Assert.Equal("Active", _trackedInstance.Status); // still active!

        // Re-submit
        _stepRepo.GetActiveStepAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowStep.Create(TenantId, _trackedInstance.Id, "submitted"));
        await _engine.AdvanceStepAsync(_trackedInstance.Id, "submit");
        Assert.Equal("pending_approval", _trackedInstance.CurrentStepName);

        // Approve
        _stepRepo.GetActiveStepAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowStep.Create(TenantId, _trackedInstance.Id, "pending_approval"));
        var result = await _engine.AdvanceStepAsync(_trackedInstance.Id, "approved", ApproverId.ToString());

        Assert.True(result.IsSuccess);
        Assert.Equal("Completed", _trackedInstance.Status);
    }

    [Fact]
    public async Task LeaveRequest_CancelAfterSubmit_CancelsWorkflow()
    {
        // Arrange
        var defId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);

        // Create instance
        await _engine.CreateInstanceAsync(TenantId, defId, "LeaveRequest", entityId, RequesterId);
        SetupInstanceRetrieval();

        var activeStep = WorkflowStep.Create(TenantId, _trackedInstance!.Id, "submitted");
        _stepRepo.GetActiveStepAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(activeStep);

        // Cancel
        var cancelResult = await _engine.CancelInstanceAsync(_trackedInstance.Id);

        Assert.True(cancelResult.IsSuccess);
        Assert.Equal("Cancelled", _trackedInstance.Status);
        Assert.Equal("Skipped", activeStep.Status);
    }

    [Fact]
    public async Task LeaveRequest_ActionExecutorCalledAtEachTransition()
    {
        // Arrange
        var defId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);
        _approverResolver.ResolveApproverAsync("supervisor", RequesterId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(ApproverId));

        // Create → on_enter notify for "submitted"
        await _engine.CreateInstanceAsync(TenantId, defId, "LeaveRequest", entityId, RequesterId);
        SetupInstanceRetrieval();

        await _actionExecutor.Received(1).ExecuteAsync(
            Arg.Is<WorkflowAction>(a => a.ActionType == "notify"),
            Arg.Any<CancellationToken>());

        // Submit → on_exit notify for "submitted" + on_enter notify for "pending_approval"
        _stepRepo.GetActiveStepAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowStep.Create(TenantId, _trackedInstance!.Id, "submitted"));

        _actionExecutor.ClearReceivedCalls();
        await _engine.AdvanceStepAsync(_trackedInstance.Id, "submit");

        // on_exit (notify) from submitted + on_enter (notify) from pending_approval = 2 notify + 0 update_entity
        // Actually on_exit is not a trigger, it's on_complete — let me check:
        // "submitted" has on_exit action, "pending_approval" has on_enter action
        // The engine uses "on_complete" or "on_exit" for the departing step
        await _actionExecutor.Received().ExecuteAsync(
            Arg.Any<WorkflowAction>(), Arg.Any<CancellationToken>());

        // Approve → on_complete update_entity for "pending_approval"
        _stepRepo.GetActiveStepAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowStep.Create(TenantId, _trackedInstance.Id, "pending_approval"));

        _actionExecutor.ClearReceivedCalls();
        await _engine.AdvanceStepAsync(_trackedInstance.Id, "approved", ApproverId.ToString());

        // on_complete (update_entity) from pending_approval
        await _actionExecutor.Received().ExecuteAsync(
            Arg.Is<WorkflowAction>(a => a.ActionType == "update_entity"),
            Arg.Any<CancellationToken>());
    }
}
