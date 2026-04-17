using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Application.Models;
using WorkBase.Modules.Workflow.Application.Services;
using WorkBase.Modules.Workflow.Domain.Entities;
using WorkBase.Modules.Workflow.Domain.Events;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application;

public interface IWorkflowEngine
{
    /// <summary>
    /// Parses and validates a workflow definition JSON.
    /// </summary>
    Result<WorkflowDefinitionModel> LoadDefinition(string definitionJson);

    /// <summary>
    /// Creates a new workflow instance from a definition, starts at the initial step.
    /// </summary>
    Task<Result<Guid>> CreateInstanceAsync(
        Guid tenantId,
        Guid definitionId,
        string entityType,
        Guid entityId,
        Guid initiatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Advances the workflow from the current step using the given outcome.
    /// Validates the transition is allowed, completes the current step, and creates the next.
    /// </summary>
    Task<Result<string>> AdvanceStepAsync(
        Guid instanceId,
        string outcome,
        string? completedBy = null,
        string? comment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current step name for a workflow instance.
    /// </summary>
    Task<Result<string>> GetCurrentStepAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns allowed outcomes for the current step of a workflow instance.
    /// </summary>
    Task<Result<List<string>>> GetAllowedOutcomesAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running workflow instance.
    /// </summary>
    Task<Result> CancelInstanceAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);
}

public sealed class WorkflowEngine(
    IWorkflowDefinitionRepository definitionRepository,
    IWorkflowInstanceRepository instanceRepository,
    IWorkflowStepRepository stepRepository,
    IWorkflowActionRepository actionRepository,
    IApprovalRequestRepository approvalRequestRepository,
    IApproverResolver approverResolver) : IWorkflowEngine
{
    public Result<WorkflowDefinitionModel> LoadDefinition(string definitionJson)
    {
        return WorkflowDefinitionParser.Parse(definitionJson);
    }

    public async Task<Result<Guid>> CreateInstanceAsync(
        Guid tenantId,
        Guid definitionId,
        string entityType,
        Guid entityId,
        Guid initiatedBy,
        CancellationToken cancellationToken = default)
    {
        var definition = await definitionRepository.GetByIdAsync(definitionId, cancellationToken);
        if (definition is null)
            return Result.Failure<Guid>(Error.NotFound("Workflow.DefinitionNotFound",
                $"Definicja workflow o id '{definitionId}' nie została znaleziona."));

        if (!definition.IsActive)
            return Result.Failure<Guid>(new Error("Workflow.DefinitionInactive",
                "Definicja workflow jest nieaktywna."));

        var parseResult = WorkflowDefinitionParser.Parse(definition.DefinitionJson);
        if (parseResult.IsFailure)
            return Result.Failure<Guid>(parseResult.Error);

        var model = parseResult.Value;

        var instance = WorkflowInstance.Create(
            tenantId,
            definitionId,
            entityType,
            entityId,
            model.InitialStep,
            initiatedBy);

        instance.RaiseDomainEvent(new WorkflowInstanceCreatedEvent(
            instance.Id, tenantId, definitionId, entityType, entityId, model.InitialStep));

        await instanceRepository.AddAsync(instance, cancellationToken);

        var step = WorkflowStep.Create(tenantId, instance.Id, model.InitialStep);
        await stepRepository.AddAsync(step, cancellationToken);

        // Execute on_enter actions for the initial step
        var stepDef = model.Steps.FirstOrDefault(s => s.Name == model.InitialStep);
        if (stepDef?.Actions is not null)
        {
            foreach (var actionDef in stepDef.Actions.Where(a => a.Trigger == "on_enter"))
            {
                var action = WorkflowAction.Create(
                    tenantId, step.Id, instance.Id, actionDef.Type,
                    actionDef.Payload is not null
                        ? System.Text.Json.JsonSerializer.Serialize(actionDef.Payload)
                        : null);
                await actionRepository.AddAsync(action, cancellationToken);
            }
        }

        // Create approval request if initial step is an approval step
        if (stepDef?.Type == "approval" && stepDef.ApproverStrategy is not null)
        {
            var approverResult = await approverResolver.ResolveApproverAsync(
                stepDef.ApproverStrategy, initiatedBy, cancellationToken);
            if (approverResult is null || approverResult.IsFailure)
                return Result.Failure<Guid>(approverResult?.Error
                    ?? new Error("Approval.ResolverFailed", "Nie udało się rozwiązać akceptanta."));

            var approvalRequest = ApprovalRequest.Create(
                tenantId, step.Id, instance.Id, initiatedBy, approverResult.Value);
            await approvalRequestRepository.AddAsync(approvalRequest, cancellationToken);
        }

        return instance.Id;
    }

    public async Task<Result<string>> AdvanceStepAsync(
        Guid instanceId,
        string outcome,
        string? completedBy = null,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
            return Result.Failure<string>(Error.NotFound("Workflow.InstanceNotFound",
                $"Instancja workflow o id '{instanceId}' nie została znaleziona."));

        if (instance.Status != "Active")
            return Result.Failure<string>(new Error("Workflow.InstanceNotActive",
                $"Instancja workflow nie jest aktywna (status: {instance.Status})."));

        var definition = await definitionRepository.GetByIdAsync(instance.DefinitionId, cancellationToken);
        if (definition is null)
            return Result.Failure<string>(Error.NotFound("Workflow.DefinitionNotFound",
                "Powiązana definicja workflow nie została znaleziona."));

        var parseResult = WorkflowDefinitionParser.Parse(definition.DefinitionJson);
        if (parseResult.IsFailure)
            return Result.Failure<string>(parseResult.Error);

        var model = parseResult.Value;
        var currentStepDef = model.Steps.FirstOrDefault(s => s.Name == instance.CurrentStepName);
        if (currentStepDef is null)
            return Result.Failure<string>(new Error("Workflow.StepNotFound",
                $"Krok '{instance.CurrentStepName}' nie istnieje w definicji workflow."));

        // Validate transition is allowed
        var transition = currentStepDef.Transitions.FirstOrDefault(t => t.Outcome == outcome);
        if (transition is null)
        {
            var allowed = currentStepDef.Transitions.Select(t => t.Outcome).ToList();
            return Result.Failure<string>(new Error("Workflow.InvalidTransition",
                $"Przejście '{outcome}' nie jest dozwolone z kroku '{instance.CurrentStepName}'. " +
                $"Dozwolone: {string.Join(", ", allowed)}."));
        }

        // Complete current step
        var activeStep = await stepRepository.GetActiveStepAsync(instanceId, cancellationToken);
        if (activeStep is not null)
        {
            activeStep.Complete(outcome, completedBy, comment);
            stepRepository.Update(activeStep);

            // Execute on_complete and on_exit actions
            if (currentStepDef.Actions is not null)
            {
                foreach (var actionDef in currentStepDef.Actions.Where(a => a.Trigger is "on_complete" or "on_exit"))
                {
                    var action = WorkflowAction.Create(
                        instance.TenantId, activeStep.Id, instanceId, actionDef.Type,
                        actionDef.Payload is not null
                            ? System.Text.Json.JsonSerializer.Serialize(actionDef.Payload)
                            : null);
                    await actionRepository.AddAsync(action, cancellationToken);
                }
            }

            activeStep.RaiseDomainEvent(new WorkflowStepCompletedEvent(
                instanceId, activeStep.Id, instance.TenantId,
                activeStep.StepName, outcome, instance.EntityType, instance.EntityId));
        }

        var targetStepName = transition.TargetStep;
        var previousStep = instance.CurrentStepName;

        // Check if target is an end step
        var targetStepDef = model.Steps.FirstOrDefault(s => s.Name == targetStepName);
        if (targetStepDef?.Type == "end")
        {
            instance.AdvanceTo(targetStepName);

            // Determine final status based on outcome
            if (outcome is "rejected" or "reject")
            {
                instance.Reject();
                instance.RaiseDomainEvent(new WorkflowInstanceRejectedEvent(
                    instanceId, instance.TenantId, instance.EntityType, instance.EntityId, targetStepName));
            }
            else
            {
                instance.Complete();
                instance.RaiseDomainEvent(new WorkflowInstanceCompletedEvent(
                    instanceId, instance.TenantId, instance.EntityType, instance.EntityId, targetStepName));
            }

            instanceRepository.Update(instance);

            // Create end step record
            var endStep = WorkflowStep.Create(instance.TenantId, instanceId, targetStepName);
            endStep.Complete(outcome, completedBy);
            await stepRepository.AddAsync(endStep, cancellationToken);

            return targetStepName;
        }

        // Advance to next step
        instance.AdvanceTo(targetStepName);
        instanceRepository.Update(instance);

        instance.RaiseDomainEvent(new WorkflowStepAdvancedEvent(
            instanceId, instance.TenantId, previousStep, targetStepName,
            instance.EntityType, instance.EntityId));

        // Create new active step
        var newStep = WorkflowStep.Create(instance.TenantId, instanceId, targetStepName);
        await stepRepository.AddAsync(newStep, cancellationToken);

        // Execute on_enter actions for new step
        if (targetStepDef?.Actions is not null)
        {
            foreach (var actionDef in targetStepDef.Actions.Where(a => a.Trigger == "on_enter"))
            {
                var action = WorkflowAction.Create(
                    instance.TenantId, newStep.Id, instanceId, actionDef.Type,
                    actionDef.Payload is not null
                        ? System.Text.Json.JsonSerializer.Serialize(actionDef.Payload)
                        : null);
                await actionRepository.AddAsync(action, cancellationToken);
            }
        }

        // Create approval request if target step is an approval step
        if (targetStepDef?.Type == "approval" && targetStepDef.ApproverStrategy is not null)
        {
            var approverResult = await approverResolver.ResolveApproverAsync(
                targetStepDef.ApproverStrategy, instance.InitiatedBy, cancellationToken);
            if (approverResult is { IsSuccess: true })
            {
                var approvalRequest = ApprovalRequest.Create(
                    instance.TenantId, newStep.Id, instanceId,
                    instance.InitiatedBy, approverResult.Value);
                await approvalRequestRepository.AddAsync(approvalRequest, cancellationToken);
            }
        }

        return targetStepName;
    }

    public async Task<Result<string>> GetCurrentStepAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
            return Result.Failure<string>(Error.NotFound("Workflow.InstanceNotFound",
                $"Instancja workflow o id '{instanceId}' nie została znaleziona."));

        return instance.CurrentStepName;
    }

    public async Task<Result<List<string>>> GetAllowedOutcomesAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
            return Result.Failure<List<string>>(Error.NotFound("Workflow.InstanceNotFound",
                $"Instancja workflow o id '{instanceId}' nie została znaleziona."));

        if (instance.Status != "Active")
            return new List<string>();

        var definition = await definitionRepository.GetByIdAsync(instance.DefinitionId, cancellationToken);
        if (definition is null)
            return Result.Failure<List<string>>(Error.NotFound("Workflow.DefinitionNotFound",
                "Powiązana definicja workflow nie została znaleziona."));

        var parseResult = WorkflowDefinitionParser.Parse(definition.DefinitionJson);
        if (parseResult.IsFailure)
            return Result.Failure<List<string>>(parseResult.Error);

        var model = parseResult.Value;
        var currentStep = model.Steps.FirstOrDefault(s => s.Name == instance.CurrentStepName);
        if (currentStep is null)
            return new List<string>();

        return currentStep.Transitions.Select(t => t.Outcome).ToList();
    }

    public async Task<Result> CancelInstanceAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
            return Result.Failure(Error.NotFound("Workflow.InstanceNotFound",
                $"Instancja workflow o id '{instanceId}' nie została znaleziona."));

        if (instance.Status != "Active")
            return Result.Failure(new Error("Workflow.InstanceNotActive",
                $"Nie można anulować instancji o statusie '{instance.Status}'."));

        // Complete active step as skipped
        var activeStep = await stepRepository.GetActiveStepAsync(instanceId, cancellationToken);
        if (activeStep is not null)
        {
            activeStep.Skip();
            stepRepository.Update(activeStep);
        }

        instance.Cancel();
        instanceRepository.Update(instance);

        return Result.Success();
    }
}
