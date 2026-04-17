using WorkBase.Contracts;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Services;

/// <summary>
/// Resolves the approver for an approval step based on the configured strategy.
/// </summary>
public interface IApproverResolver
{
    /// <summary>
    /// Resolves the approver employee ID for a given workflow step.
    /// </summary>
    /// <param name="strategy">The approver strategy (e.g. "supervisor").</param>
    /// <param name="initiatedByUserId">The user ID who initiated the workflow (Keycloak/Identity).</param>
    Task<Result<Guid>> ResolveApproverAsync(
        string strategy,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default);
}

public sealed class ApproverResolver(ISupervisorLookupService supervisorLookup) : IApproverResolver
{
    public async Task<Result<Guid>> ResolveApproverAsync(
        string strategy,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default)
    {
        return strategy switch
        {
            "supervisor" => await ResolveSupervisorAsync(initiatedByUserId, cancellationToken),
            _ => Result.Failure<Guid>(new Error("Approval.UnknownStrategy",
                $"Nieznana strategia akceptanta: '{strategy}'."))
        };
    }

    private async Task<Result<Guid>> ResolveSupervisorAsync(
        Guid initiatedByUserId,
        CancellationToken cancellationToken)
    {
        var employeeId = await supervisorLookup.GetEmployeeIdByUserIdAsync(initiatedByUserId, cancellationToken);
        if (employeeId is null)
            return Result.Failure<Guid>(Error.NotFound("Approval.EmployeeNotFound",
                "Nie znaleziono pracownika powiązanego z użytkownikiem inicjującym workflow."));

        var supervisorId = await supervisorLookup.GetSupervisorEmployeeIdAsync(employeeId.Value, cancellationToken);
        if (supervisorId is null)
            return Result.Failure<Guid>(Error.NotFound("Approval.SupervisorNotFound",
                "Nie znaleziono przełożonego dla pracownika inicjującego workflow."));

        return supervisorId.Value;
    }
}
