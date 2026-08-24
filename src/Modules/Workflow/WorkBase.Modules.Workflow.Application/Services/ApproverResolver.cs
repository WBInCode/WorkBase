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
        // Inicjator bywa identyfikatorem konta (obieg z API), a bywa identyfikatorem
        // pracownika (wniosek urlopowy) — bez tego drugiego przypadku wnioski urlopowe
        // nigdy nie znajdowały przelozonego.
        var employeeId = await supervisorLookup.GetEmployeeIdByUserIdAsync(initiatedByUserId, cancellationToken)
            ?? initiatedByUserId;

        var supervisorId = await supervisorLookup.GetSupervisorEmployeeIdAsync(employeeId, cancellationToken);
        if (supervisorId is null)
            return Result.Failure<Guid>(Error.NotFound("Approval.SupervisorNotFound",
                "Nie znaleziono przełożonego dla pracownika inicjującego workflow."));

        return await UwzglednijZastepstwoAsync(supervisorId.Value, cancellationToken);
    }

    /// <summary>
    /// Podmienia akceptanta na jego zastępcę, jeśli ktoś go dziś zastępuje.
    /// </summary>
    /// <remarks>
    /// To jedyne miejsce w systemie, w którym rozstrzyga się „kto to zatwierdza", więc zastępstwo
    /// wystarczy uwzględnić tutaj — obejmuje wszystkie obiegi, nie tylko urlopowe.
    ///
    /// Łańcuch zastępstw jest podążany, bo zastępca też bywa nieobecny (klasyczny przypadek:
    /// kierownik i jego zastępca jadą na to samo szkolenie). Limit i zbiór odwiedzonych chronią
    /// przed pętlą, gdy dwie osoby wskażą się nawzajem — wtedy wniosek zostaje przy ostatniej
    /// osobie w łańcuchu, co jest gorsze niż nic, ale lepsze niż zawieszenie obiegu.
    /// </remarks>
    private async Task<Guid> UwzglednijZastepstwoAsync(Guid akceptant, CancellationToken cancellationToken)
    {
        const int maksymalnaDlugoscLancucha = 5;

        var dzis = DateOnly.FromDateTime(DateTime.UtcNow);
        var odwiedzeni = new HashSet<Guid> { akceptant };
        var biezacy = akceptant;

        for (var krok = 0; krok < maksymalnaDlugoscLancucha; krok++)
        {
            var zastepca = await supervisorLookup.GetZastepceAsync(biezacy, dzis, cancellationToken);
            if (zastepca is null || !odwiedzeni.Add(zastepca.Value))
                return biezacy;

            biezacy = zastepca.Value;
        }

        return biezacy;
    }
}
