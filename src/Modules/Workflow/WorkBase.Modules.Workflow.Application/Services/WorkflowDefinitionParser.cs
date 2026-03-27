using System.Text.Json;
using WorkBase.Modules.Workflow.Application.Models;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Services;

/// <summary>
/// Parses and validates workflow definition JSON into the in-memory model.
/// </summary>
public static class WorkflowDefinitionParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static Result<WorkflowDefinitionModel> Parse(string json)
    {
        WorkflowDefinitionModel? model;
        try
        {
            model = JsonSerializer.Deserialize<WorkflowDefinitionModel>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            return Result.Failure<WorkflowDefinitionModel>(
                new Error("Workflow.InvalidJson", $"Nieprawidłowy format JSON: {ex.Message}"));
        }

        if (model is null)
            return Result.Failure<WorkflowDefinitionModel>(
                new Error("Workflow.EmptyDefinition", "Definicja workflow jest pusta."));

        return Validate(model);
    }

    private static Result<WorkflowDefinitionModel> Validate(WorkflowDefinitionModel model)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(model.Name))
            errors.Add(new ValidationError("name", "Nazwa workflow jest wymagana."));

        if (string.IsNullOrWhiteSpace(model.EntityType))
            errors.Add(new ValidationError("entityType", "Typ encji jest wymagany."));

        if (string.IsNullOrWhiteSpace(model.InitialStep))
            errors.Add(new ValidationError("initialStep", "Krok początkowy jest wymagany."));

        if (model.Steps.Count == 0)
        {
            errors.Add(new ValidationError("steps", "Definicja musi zawierać co najmniej jeden krok."));
            return errors.Count > 0
                ? Result.ValidationFailure<WorkflowDefinitionModel>(errors)
                : model;
        }

        var stepNames = model.Steps.Select(s => s.Name).ToHashSet();

        if (!stepNames.Contains(model.InitialStep))
            errors.Add(new ValidationError("initialStep",
                $"Krok początkowy '{model.InitialStep}' nie istnieje w definicji."));

        var hasEndStep = false;

        foreach (var step in model.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Name))
            {
                errors.Add(new ValidationError("steps", "Każdy krok musi mieć nazwę."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(step.Type))
                errors.Add(new ValidationError($"steps[{step.Name}].type", "Typ kroku jest wymagany."));

            if (step.Type == "end")
            {
                hasEndStep = true;
                continue;
            }

            if (step.Transitions.Count == 0 && step.Type != "end")
                errors.Add(new ValidationError($"steps[{step.Name}].transitions",
                    $"Krok '{step.Name}' (typ: {step.Type}) musi mieć co najmniej jedno przejście."));

            foreach (var transition in step.Transitions)
            {
                if (string.IsNullOrWhiteSpace(transition.Outcome))
                    errors.Add(new ValidationError($"steps[{step.Name}].transitions",
                        "Każde przejście musi mieć outcome."));

                if (string.IsNullOrWhiteSpace(transition.TargetStep))
                    errors.Add(new ValidationError($"steps[{step.Name}].transitions",
                        "Każde przejście musi mieć targetStep."));
                else if (!stepNames.Contains(transition.TargetStep))
                    errors.Add(new ValidationError($"steps[{step.Name}].transitions",
                        $"Krok docelowy '{transition.TargetStep}' nie istnieje w definicji."));
            }
        }

        if (!hasEndStep)
            errors.Add(new ValidationError("steps", "Definicja musi zawierać co najmniej jeden krok końcowy (type: 'end')."));

        return errors.Count > 0
            ? Result.ValidationFailure<WorkflowDefinitionModel>(errors)
            : model;
    }
}
