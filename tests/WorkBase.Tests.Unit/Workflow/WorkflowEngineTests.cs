using NSubstitute;
using WorkBase.Modules.Workflow.Application;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Application.Services;
using WorkBase.Modules.Workflow.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Unit.Workflow;

public class WorkflowEngineTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private const string LeaveRequestDefinitionJson = """
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
                    { "type": "notify", "trigger": "on_enter", "payload": { "template": "leave_submitted" } }
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

    public WorkflowEngineTests()
    {
        _engine = new WorkflowEngine(_definitionRepo, _instanceRepo, _stepRepo, _actionRepo, _approvalRequestRepo, _approverResolver, _actionExecutor);
    }

    private WorkflowDefinition CreateDefinition(Guid? id = null)
    {
        var def = WorkflowDefinition.Create(TenantId, "leave-request-v1", LeaveRequestDefinitionJson, "Leave request workflow");
        if (id.HasValue)
        {
            // Use reflection to set Id for testing
            typeof(WorkflowDefinition).BaseType!.BaseType!
                .GetProperty("Id")!.SetValue(def, id.Value);
        }
        return def;
    }

    // --- LoadDefinition ---

    [Fact]
    public void LoadDefinition_ValidJson_ReturnsModel()
    {
        var result = _engine.LoadDefinition(LeaveRequestDefinitionJson);

        Assert.True(result.IsSuccess);
        Assert.Equal("leave-request-v1", result.Value.Name);
        Assert.Equal(4, result.Value.Steps.Count);
    }

    [Fact]
    public void LoadDefinition_InvalidJson_ReturnsFailure()
    {
        var result = _engine.LoadDefinition("not json");

        Assert.True(result.IsFailure);
    }

    // --- CreateInstance ---

    [Fact]
    public async Task CreateInstance_ValidDefinition_CreatesInstanceAndStep()
    {
        var defId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await _engine.CreateInstanceAsync(TenantId, defId, "LeaveRequest", entityId, UserId);

        Assert.True(result.IsSuccess);
        await _instanceRepo.Received(1).AddAsync(Arg.Is<WorkflowInstance>(i =>
            i.TenantId == TenantId &&
            i.DefinitionId == defId &&
            i.EntityType == "LeaveRequest" &&
            i.EntityId == entityId &&
            i.CurrentStepName == "submitted" &&
            i.Status == "Active"), Arg.Any<CancellationToken>());

        await _stepRepo.Received(1).AddAsync(Arg.Is<WorkflowStep>(s =>
            s.StepName == "submitted" &&
            s.Status == "Active"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateInstance_ValidDefinition_RaisesDomainEvent()
    {
        var defId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);

        WorkflowInstance? capturedInstance = null;
        await _instanceRepo.AddAsync(Arg.Do<WorkflowInstance>(i => capturedInstance = i), Arg.Any<CancellationToken>());

        await _engine.CreateInstanceAsync(TenantId, defId, "LeaveRequest", entityId, UserId);

        Assert.NotNull(capturedInstance);
        Assert.Single(capturedInstance!.DomainEvents);
    }

    [Fact]
    public async Task CreateInstance_ExecutesOnEnterActions()
    {
        var defId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);

        await _engine.CreateInstanceAsync(TenantId, defId, "LeaveRequest", entityId, UserId);

        // "submitted" step has one on_enter action (notify)
        await _actionRepo.Received(1).AddAsync(Arg.Is<WorkflowAction>(a =>
            a.ActionType == "notify"), Arg.Any<CancellationToken>());

        // Action executor should have been called for the notify action
        await _actionExecutor.Received(1).ExecuteAsync(
            Arg.Is<WorkflowAction>(a => a.ActionType == "notify"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateInstance_DefinitionNotFound_ReturnsNotFound()
    {
        var defId = Guid.NewGuid();
        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns((WorkflowDefinition?)null);

        var result = await _engine.CreateInstanceAsync(TenantId, defId, "LeaveRequest", Guid.NewGuid(), UserId);

        Assert.True(result.IsFailure);
        Assert.Equal("Workflow.DefinitionNotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateInstance_InactiveDefinition_ReturnsFailure()
    {
        var defId = Guid.NewGuid();
        var definition = CreateDefinition(defId);
        definition.Deactivate();

        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await _engine.CreateInstanceAsync(TenantId, defId, "LeaveRequest", Guid.NewGuid(), UserId);

        Assert.True(result.IsFailure);
        Assert.Equal("Workflow.DefinitionInactive", result.Error.Code);
    }

    // --- AdvanceStep: Happy path (submit → approve → done) ---

    [Fact]
    public async Task AdvanceStep_ValidTransition_AdvancesToNextStep()
    {
        var instanceId = Guid.NewGuid();
        var defId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        var instance = WorkflowInstance.Create(TenantId, defId, "LeaveRequest", Guid.NewGuid(), "submitted", UserId);
        SetId(instance, instanceId);

        var activeStep = WorkflowStep.Create(TenantId, instanceId, "submitted");

        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);
        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);
        _stepRepo.GetActiveStepAsync(instanceId, Arg.Any<CancellationToken>()).Returns(activeStep);

        var result = await _engine.AdvanceStepAsync(instanceId, "submit", UserId.ToString());

        Assert.True(result.IsSuccess);
        Assert.Equal("pending_approval", result.Value);
        Assert.Equal("pending_approval", instance.CurrentStepName);
        Assert.Equal("Active", instance.Status);
    }

    [Fact]
    public async Task AdvanceStep_ToEndStepApproved_CompletesInstance()
    {
        var instanceId = Guid.NewGuid();
        var defId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        var instance = WorkflowInstance.Create(TenantId, defId, "LeaveRequest", Guid.NewGuid(), "pending_approval", UserId);
        SetId(instance, instanceId);

        var activeStep = WorkflowStep.Create(TenantId, instanceId, "pending_approval");

        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);
        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);
        _stepRepo.GetActiveStepAsync(instanceId, Arg.Any<CancellationToken>()).Returns(activeStep);

        var result = await _engine.AdvanceStepAsync(instanceId, "approved", UserId.ToString());

        Assert.True(result.IsSuccess);
        Assert.Equal("approved", result.Value);
        Assert.Equal("Completed", instance.Status);
        Assert.NotNull(instance.CompletedAt);
    }

    [Fact]
    public async Task AdvanceStep_ToEndStepRejected_RejectsInstance()
    {
        var instanceId = Guid.NewGuid();
        var defId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        var instance = WorkflowInstance.Create(TenantId, defId, "LeaveRequest", Guid.NewGuid(), "pending_approval", UserId);
        SetId(instance, instanceId);

        var activeStep = WorkflowStep.Create(TenantId, instanceId, "pending_approval");

        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);
        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);
        _stepRepo.GetActiveStepAsync(instanceId, Arg.Any<CancellationToken>()).Returns(activeStep);

        var result = await _engine.AdvanceStepAsync(instanceId, "rejected", UserId.ToString(), "Nie zgadzam się");

        Assert.True(result.IsSuccess);
        Assert.Equal("rejected", result.Value);
        Assert.Equal("Rejected", instance.Status);
    }

    // --- AdvanceStep: Edge cases ---

    [Fact]
    public async Task AdvanceStep_InvalidOutcome_ReturnsFailure()
    {
        var instanceId = Guid.NewGuid();
        var defId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        var instance = WorkflowInstance.Create(TenantId, defId, "LeaveRequest", Guid.NewGuid(), "submitted", UserId);
        SetId(instance, instanceId);

        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);
        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await _engine.AdvanceStepAsync(instanceId, "nonexistent_outcome");

        Assert.True(result.IsFailure);
        Assert.Equal("Workflow.InvalidTransition", result.Error.Code);
    }

    [Fact]
    public async Task AdvanceStep_InstanceNotActive_ReturnsFailure()
    {
        var instanceId = Guid.NewGuid();
        var defId = Guid.NewGuid();

        var instance = WorkflowInstance.Create(TenantId, defId, "LeaveRequest", Guid.NewGuid(), "approved", UserId);
        SetId(instance, instanceId);
        instance.Complete();

        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _engine.AdvanceStepAsync(instanceId, "submit");

        Assert.True(result.IsFailure);
        Assert.Equal("Workflow.InstanceNotActive", result.Error.Code);
    }

    [Fact]
    public async Task AdvanceStep_InstanceNotFound_ReturnsNotFound()
    {
        var instanceId = Guid.NewGuid();
        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns((WorkflowInstance?)null);

        var result = await _engine.AdvanceStepAsync(instanceId, "submit");

        Assert.True(result.IsFailure);
        Assert.Equal("Workflow.InstanceNotFound", result.Error.Code);
    }

    [Fact]
    public async Task AdvanceStep_ReturnToSubmitted_CyclesBackToStart()
    {
        var instanceId = Guid.NewGuid();
        var defId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        var instance = WorkflowInstance.Create(TenantId, defId, "LeaveRequest", Guid.NewGuid(), "pending_approval", UserId);
        SetId(instance, instanceId);

        var activeStep = WorkflowStep.Create(TenantId, instanceId, "pending_approval");

        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);
        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);
        _stepRepo.GetActiveStepAsync(instanceId, Arg.Any<CancellationToken>()).Returns(activeStep);

        var result = await _engine.AdvanceStepAsync(instanceId, "returned", UserId.ToString(), "Popraw daty");

        Assert.True(result.IsSuccess);
        Assert.Equal("submitted", result.Value);
        Assert.Equal("Active", instance.Status);
        Assert.Equal("submitted", instance.CurrentStepName);
    }

    [Fact]
    public async Task AdvanceStep_CompletesCurrentStep_WithOutcomeAndComment()
    {
        var instanceId = Guid.NewGuid();
        var defId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        var instance = WorkflowInstance.Create(TenantId, defId, "LeaveRequest", Guid.NewGuid(), "submitted", UserId);
        SetId(instance, instanceId);

        var activeStep = WorkflowStep.Create(TenantId, instanceId, "submitted");

        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);
        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);
        _stepRepo.GetActiveStepAsync(instanceId, Arg.Any<CancellationToken>()).Returns(activeStep);

        await _engine.AdvanceStepAsync(instanceId, "submit", UserId.ToString(), "Proszę o urlop");

        Assert.Equal("Completed", activeStep.Status);
        Assert.Equal("submit", activeStep.Outcome);
        Assert.Equal(UserId.ToString(), activeStep.CompletedBy);
        Assert.Equal("Proszę o urlop", activeStep.Comment);
    }

    // --- Full flow: submit → approve → done ---

    [Fact]
    public async Task FullFlow_SubmitThenApprove_CompletesWorkflow()
    {
        var instanceId = Guid.NewGuid();
        var defId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        // Step 1: Create instance at "submitted"
        var instance = WorkflowInstance.Create(TenantId, defId, "LeaveRequest", Guid.NewGuid(), "submitted", UserId);
        SetId(instance, instanceId);

        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);

        // Step 2: Advance from "submitted" to "pending_approval"
        var submittedStep = WorkflowStep.Create(TenantId, instanceId, "submitted");
        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);
        _stepRepo.GetActiveStepAsync(instanceId, Arg.Any<CancellationToken>()).Returns(submittedStep);

        var advanceResult1 = await _engine.AdvanceStepAsync(instanceId, "submit", UserId.ToString());
        Assert.True(advanceResult1.IsSuccess);
        Assert.Equal("pending_approval", advanceResult1.Value);
        Assert.Equal("Active", instance.Status);

        // Step 3: Advance from "pending_approval" to "approved" (end)
        var pendingStep = WorkflowStep.Create(TenantId, instanceId, "pending_approval");
        _stepRepo.GetActiveStepAsync(instanceId, Arg.Any<CancellationToken>()).Returns(pendingStep);

        var advanceResult2 = await _engine.AdvanceStepAsync(instanceId, "approved", UserId.ToString(), "Zatwierdzam");
        Assert.True(advanceResult2.IsSuccess);
        Assert.Equal("approved", advanceResult2.Value);
        Assert.Equal("Completed", instance.Status);
        Assert.NotNull(instance.CompletedAt);
    }

    // --- Full flow: submit → reject ---

    [Fact]
    public async Task FullFlow_SubmitThenReject_RejectsWorkflow()
    {
        var instanceId = Guid.NewGuid();
        var defId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        var instance = WorkflowInstance.Create(TenantId, defId, "LeaveRequest", Guid.NewGuid(), "submitted", UserId);
        SetId(instance, instanceId);

        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);

        // Step 1: submit → pending_approval
        var submittedStep = WorkflowStep.Create(TenantId, instanceId, "submitted");
        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);
        _stepRepo.GetActiveStepAsync(instanceId, Arg.Any<CancellationToken>()).Returns(submittedStep);

        await _engine.AdvanceStepAsync(instanceId, "submit", UserId.ToString());

        // Step 2: reject → rejected (end)
        var pendingStep = WorkflowStep.Create(TenantId, instanceId, "pending_approval");
        _stepRepo.GetActiveStepAsync(instanceId, Arg.Any<CancellationToken>()).Returns(pendingStep);

        var result = await _engine.AdvanceStepAsync(instanceId, "rejected", UserId.ToString(), "Brak dni");

        Assert.True(result.IsSuccess);
        Assert.Equal("rejected", result.Value);
        Assert.Equal("Rejected", instance.Status);
    }

    // --- Full flow: submit → return → re-submit → approve ---

    [Fact]
    public async Task FullFlow_SubmitReturnResubmitApprove_CompletesWorkflow()
    {
        var instanceId = Guid.NewGuid();
        var defId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        var instance = WorkflowInstance.Create(TenantId, defId, "LeaveRequest", Guid.NewGuid(), "submitted", UserId);
        SetId(instance, instanceId);

        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);
        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);

        // Step 1: submit → pending_approval
        _stepRepo.GetActiveStepAsync(instanceId, Arg.Any<CancellationToken>()).Returns(WorkflowStep.Create(TenantId, instanceId, "submitted"));
        await _engine.AdvanceStepAsync(instanceId, "submit");
        Assert.Equal("pending_approval", instance.CurrentStepName);

        // Step 2: return → submitted (cycle back)
        _stepRepo.GetActiveStepAsync(instanceId, Arg.Any<CancellationToken>()).Returns(WorkflowStep.Create(TenantId, instanceId, "pending_approval"));
        await _engine.AdvanceStepAsync(instanceId, "returned", UserId.ToString(), "Popraw daty");
        Assert.Equal("submitted", instance.CurrentStepName);
        Assert.Equal("Active", instance.Status);

        // Step 3: re-submit → pending_approval
        _stepRepo.GetActiveStepAsync(instanceId, Arg.Any<CancellationToken>()).Returns(WorkflowStep.Create(TenantId, instanceId, "submitted"));
        await _engine.AdvanceStepAsync(instanceId, "submit");
        Assert.Equal("pending_approval", instance.CurrentStepName);

        // Step 4: approve → approved (end)
        _stepRepo.GetActiveStepAsync(instanceId, Arg.Any<CancellationToken>()).Returns(WorkflowStep.Create(TenantId, instanceId, "pending_approval"));
        var finalResult = await _engine.AdvanceStepAsync(instanceId, "approved");
        Assert.True(finalResult.IsSuccess);
        Assert.Equal("Completed", instance.Status);
    }

    // --- GetCurrentStep ---

    [Fact]
    public async Task GetCurrentStep_ExistingInstance_ReturnsStepName()
    {
        var instanceId = Guid.NewGuid();
        var instance = WorkflowInstance.Create(TenantId, Guid.NewGuid(), "LeaveRequest", Guid.NewGuid(), "pending_approval", UserId);
        SetId(instance, instanceId);

        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _engine.GetCurrentStepAsync(instanceId);

        Assert.True(result.IsSuccess);
        Assert.Equal("pending_approval", result.Value);
    }

    [Fact]
    public async Task GetCurrentStep_NonExistentInstance_ReturnsNotFound()
    {
        _instanceRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WorkflowInstance?)null);

        var result = await _engine.GetCurrentStepAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Workflow.InstanceNotFound", result.Error.Code);
    }

    // --- GetAllowedOutcomes ---

    [Fact]
    public async Task GetAllowedOutcomes_ActiveInstance_ReturnsOutcomes()
    {
        var instanceId = Guid.NewGuid();
        var defId = Guid.NewGuid();
        var definition = CreateDefinition(defId);

        var instance = WorkflowInstance.Create(TenantId, defId, "LeaveRequest", Guid.NewGuid(), "pending_approval", UserId);
        SetId(instance, instanceId);

        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);
        _definitionRepo.GetByIdAsync(defId, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await _engine.GetAllowedOutcomesAsync(instanceId);

        Assert.True(result.IsSuccess);
        Assert.Contains("approved", result.Value);
        Assert.Contains("rejected", result.Value);
        Assert.Contains("returned", result.Value);
        Assert.Equal(3, result.Value.Count);
    }

    [Fact]
    public async Task GetAllowedOutcomes_CompletedInstance_ReturnsEmpty()
    {
        var instanceId = Guid.NewGuid();
        var instance = WorkflowInstance.Create(TenantId, Guid.NewGuid(), "LeaveRequest", Guid.NewGuid(), "approved", UserId);
        SetId(instance, instanceId);
        instance.Complete();

        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _engine.GetAllowedOutcomesAsync(instanceId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // --- CancelInstance ---

    [Fact]
    public async Task CancelInstance_ActiveInstance_CancelsSuccessfully()
    {
        var instanceId = Guid.NewGuid();
        var instance = WorkflowInstance.Create(TenantId, Guid.NewGuid(), "LeaveRequest", Guid.NewGuid(), "submitted", UserId);
        SetId(instance, instanceId);

        var activeStep = WorkflowStep.Create(TenantId, instanceId, "submitted");

        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);
        _stepRepo.GetActiveStepAsync(instanceId, Arg.Any<CancellationToken>()).Returns(activeStep);

        var result = await _engine.CancelInstanceAsync(instanceId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cancelled", instance.Status);
        Assert.Equal("Skipped", activeStep.Status);
    }

    [Fact]
    public async Task CancelInstance_CompletedInstance_ReturnsFailure()
    {
        var instanceId = Guid.NewGuid();
        var instance = WorkflowInstance.Create(TenantId, Guid.NewGuid(), "LeaveRequest", Guid.NewGuid(), "approved", UserId);
        SetId(instance, instanceId);
        instance.Complete();

        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _engine.CancelInstanceAsync(instanceId);

        Assert.True(result.IsFailure);
        Assert.Equal("Workflow.InstanceNotActive", result.Error.Code);
    }

    // --- Helper ---
    private static void SetId<T>(T entity, Guid id) where T : class
    {
        // Walk up to Entity<Guid> and set Id
        var type = entity.GetType();
        while (type is not null)
        {
            var idProp = type.GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            if (idProp is not null && idProp.PropertyType == typeof(Guid))
            {
                idProp.SetValue(entity, id);
                return;
            }
            type = type.BaseType;
        }

        // Fallback: set via any type that has Id
        entity.GetType().BaseType?.BaseType?
            .GetProperty("Id")?.SetValue(entity, id);
    }
}
