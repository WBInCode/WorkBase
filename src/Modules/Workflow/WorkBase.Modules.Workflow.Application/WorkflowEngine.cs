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

    /// <summary>
    /// Sets context data on a workflow instance for condition evaluation.
    /// </summary>
    Task<Result> SetInstanceContextAsync(
        Guid instanceId,
        string contextJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a condition expression against instance context.
    /// </summary>
    Task<Result<bool>> EvaluateConditionAsync(
        Guid instanceId,
        string expression,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Advances a specific parallel branch to its next step.
    /// </summary>
    Task<Result<string>> AdvanceBranchAsync(
        Guid instanceId,
        Guid branchId,
        string outcome,
        string? completedBy = null,
        string? comment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active branches for a workflow instance.
    /// </summary>
    Task<Result<List<WorkflowBranchDto>>> GetActiveBranchesAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default);
}

public sealed record WorkflowBranchDto(
    Guid Id, string BranchName, string? CurrentStepName, string Status, DateTime StartedAt, DateTime? CompletedAt);

public sealed class WorkflowEngine(
    IWorkflowDefinitionRepository definitionRepository,
    IWorkflowInstanceRepository instanceRepository,
    IWorkflowStepRepository stepRepository,
    IWorkflowActionRepository actionRepository,
    IApprovalRequestRepository approvalRequestRepository,
    IWorkflowBranchRepository branchRepository,
    IApproverResolver approverResolver,
    IWorkflowActionExecutor actionExecutor,
    IConditionEvaluator conditionEvaluator) : IWorkflowEngine
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
                await actionExecutor.ExecuteAsync(action, cancellationToken);
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

        // Validate transition is allowed (with condition evaluation)
        var transition = currentStepDef.Transitions.FirstOrDefault(t =>
            t.Outcome == outcome &&
            (string.IsNullOrEmpty(t.Condition) || conditionEvaluator.Evaluate(t.Condition, instance.ContextJson)));
        if (transition is null)
        {
            var allowed = currentStepDef.Transitions
                .Where(t => string.IsNullOrEmpty(t.Condition) || conditionEvaluator.Evaluate(t.Condition, instance.ContextJson))
                .Select(t => t.Outcome).ToList();
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
                    await actionExecutor.ExecuteAsync(action, cancellationToken);
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
                await actionExecutor.ExecuteAsync(action, cancellationToken);
            }
        }

        // Create approval request if target step is an approval step
        if (targetStepDef?.Type == "approval" && targetStepDef.ApproverStrategy is not null)
        {
            // Multi-level approval: create multiple approval requests with Order
            var levels = targetStepDef.ApproverLevels ?? 1;
            for (var level = 0; level < levels; level++)
            {
                var approverResult = await approverResolver.ResolveApproverAsync(
                    targetStepDef.ApproverStrategy, instance.InitiatedBy, cancellationToken);
                if (approverResult is { IsSuccess: true })
                {
                    var approvalRequest = ApprovalRequest.Create(
                        instance.TenantId, newStep.Id, instanceId,
                        instance.InitiatedBy, approverResult.Value, order: level);
                    await approvalRequestRepository.AddAsync(approvalRequest, cancellationToken);
                }
            }
        }

        // Handle parallel gateway: create branches
        if (targetStepDef?.Type == "parallel_gateway" && targetStepDef.ParallelBranches is { Count: > 0 })
        {
            foreach (var branchDef in targetStepDef.ParallelBranches)
            {
                // Evaluate branch condition if present
                if (!string.IsNullOrEmpty(branchDef.Condition) &&
                    !conditionEvaluator.Evaluate(branchDef.Condition, instance.ContextJson))
                {
                    var skippedBranch = WorkflowBranch.Create(
                        instance.TenantId, instanceId, targetStepName, branchDef.Name, branchDef.Steps[0]);
                    skippedBranch.Skip();
                    await branchRepository.AddAsync(skippedBranch, cancellationToken);
                    continue;
                }

                if (branchDef.Steps.Count == 0) continue;

                var branch = WorkflowBranch.Create(
                    instance.TenantId, instanceId, targetStepName, branchDef.Name, branchDef.Steps[0]);
                await branchRepository.AddAsync(branch, cancellationToken);

                // Create the first step in the branch
                var branchStep = WorkflowStep.Create(instance.TenantId, instanceId, branchDef.Steps[0]);
                await stepRepository.AddAsync(branchStep, cancellationToken);
            }
        }

        // Handle condition gateway: evaluate conditions on transitions and auto-advance
        if (targetStepDef?.Type == "condition_gateway")
        {
            var matchedTransition = targetStepDef.Transitions.FirstOrDefault(t =>
                !string.IsNullOrEmpty(t.Condition) && conditionEvaluator.Evaluate(t.Condition, instance.ContextJson));

            // Fall back to default (no condition) transition
            matchedTransition ??= targetStepDef.Transitions.FirstOrDefault(t => string.IsNullOrEmpty(t.Condition));

            if (matchedTransition is not null)
            {
                newStep.Complete(matchedTransition.Outcome, "system");
                stepRepository.Update(newStep);
                return await AdvanceStepAsync(instanceId, matchedTransition.Outcome, "system", "Auto-routed by condition", cancellationToken);
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

    public async Task<Result> SetInstanceContextAsync(
        Guid instanceId,
        string contextJson,
        CancellationToken cancellationToken = default)
    {
        var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
            return Result.Failure(Error.NotFound("Workflow.InstanceNotFound",
                $"Instancja workflow o id '{instanceId}' nie została znaleziona."));

        instance.SetContext(contextJson);
        instanceRepository.Update(instance);
        return Result.Success();
    }

    public async Task<Result<bool>> EvaluateConditionAsync(
        Guid instanceId,
        string expression,
        CancellationToken cancellationToken = default)
    {
        var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
            return Result.Failure<bool>(Error.NotFound("Workflow.InstanceNotFound",
                $"Instancja workflow o id '{instanceId}' nie została znaleziona."));

        return conditionEvaluator.Evaluate(expression, instance.ContextJson);
    }

    public async Task<Result<string>> AdvanceBranchAsync(
        Guid instanceId,
        Guid branchId,
        string outcome,
        string? completedBy = null,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
            return Result.Failure<string>(Error.NotFound("Workflow.InstanceNotFound",
                $"Instancja workflow o id '{instanceId}' nie została znaleziona."));

        var branch = await branchRepository.GetByIdAsync(branchId, cancellationToken);
        if (branch is null || branch.InstanceId != instanceId)
            return Result.Failure<string>(Error.NotFound("Workflow.BranchNotFound",
                $"Gałąź workflow o id '{branchId}' nie została znaleziona."));

        if (branch.Status != "Active")
            return Result.Failure<string>(new Error("Workflow.BranchNotActive",
                "Gałąź workflow nie jest aktywna."));

        var definition = await definitionRepository.GetByIdAsync(instance.DefinitionId, cancellationToken);
        if (definition is null)
            return Result.Failure<string>(Error.NotFound("Workflow.DefinitionNotFound",
                "Powiązana definicja workflow nie została znaleziona."));

        var parseResult = WorkflowDefinitionParser.Parse(definition.DefinitionJson);
        if (parseResult.IsFailure)
            return Result.Failure<string>(parseResult.Error);

        var model = parseResult.Value;
        var gatewayStep = model.Steps.FirstOrDefault(s => s.Name == branch.GatewayStepName);
        var branchDef = gatewayStep?.ParallelBranches?.FirstOrDefault(b => b.Name == branch.BranchName);
        if (branchDef is null)
            return Result.Failure<string>(new Error("Workflow.BranchDefinitionNotFound",
                "Definicja gałęzi nie została znaleziona."));

        // Find current position in branch steps
        var currentIdx = branchDef.Steps.IndexOf(branch.CurrentStepName!);
        if (currentIdx < 0)
            return Result.Failure<string>(new Error("Workflow.BranchStepNotFound",
                "Bieżący krok gałęzi nie został znaleziony."));

        // Complete current step
        var activeStep = await stepRepository.GetActiveStepAsync(instanceId, cancellationToken);
        if (activeStep is not null && activeStep.StepName == branch.CurrentStepName)
        {
            activeStep.Complete(outcome, completedBy, comment);
            stepRepository.Update(activeStep);
        }

        // Check if this was the last step in the branch
        if (currentIdx >= branchDef.Steps.Count - 1)
        {
            branch.Complete();
            branchRepository.Update(branch);

            // Check if all branches for this gateway are done
            var allBranches = await branchRepository.GetByInstanceAndGatewayAsync(
                instanceId, branch.GatewayStepName, cancellationToken);
            var joinType = gatewayStep?.JoinType ?? "all";
            var shouldConverge = joinType == "any"
                ? allBranches.Any(b => b.Status == "Completed")
                : allBranches.All(b => b.Status is "Completed" or "Skipped");

            if (shouldConverge && gatewayStep?.ConvergenceStep is not null)
            {
                // Advance main instance to convergence step
                return await AdvanceStepAsync(instanceId, "converged", completedBy, comment, cancellationToken);
            }

            return $"branch:{branch.BranchName}:completed";
        }

        // Advance to next step in branch
        var nextStepName = branchDef.Steps[currentIdx + 1];
        branch.AdvanceTo(nextStepName);
        branchRepository.Update(branch);

        var newStep = WorkflowStep.Create(instance.TenantId, instanceId, nextStepName);
        await stepRepository.AddAsync(newStep, cancellationToken);

        return nextStepName;
    }

    public async Task<Result<List<WorkflowBranchDto>>> GetActiveBranchesAsync(
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
            return Result.Failure<List<WorkflowBranchDto>>(Error.NotFound("Workflow.InstanceNotFound",
                $"Instancja workflow o id '{instanceId}' nie została znaleziona."));

        var branches = await branchRepository.GetActiveByInstanceAsync(instanceId, cancellationToken);
        return branches.Select(b => new WorkflowBranchDto(
            b.Id, b.BranchName, b.CurrentStepName, b.Status, b.StartedAt, b.CompletedAt)).ToList();
    }
}
