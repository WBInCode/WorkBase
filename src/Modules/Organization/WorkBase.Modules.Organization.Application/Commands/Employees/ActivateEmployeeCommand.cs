using WorkBase.Contracts;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

/// <summary>
/// Przywrócenie pracownika: powrót po przerwie albo cofnięcie omyłkowego zwolnienia.
/// </summary>
/// <remarks>
/// <c>Employee.Activate()</c> istniało od dawna i <b>nie miało ani jednego wołającego</b> — dało
/// się kogoś zwolnić, ale nie dało się tego cofnąć z aplikacji. Dopóki zwolnienie zmieniało tylko
/// status, było to uciążliwe. Odkąd odbiera też dostęp do konta, byłoby nie do odkręcenia bez
/// konsoli Keycloaka, więc droga powrotna jest częścią tej samej zmiany.
/// </remarks>
public sealed record ActivateEmployeeCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class ActivateEmployeeHandler(
    IEmployeeRepository repository,
    IEmployeeAccessProvisioningQueue accessProvisioningQueue)
    : ICommandHandler<ActivateEmployeeCommand>
{
    public async Task<Result> Handle(ActivateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (employee is null || employee.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("Employee.NotFound", "Pracownik nie został znaleziony."));

        employee.Activate();
        repository.Update(employee);

        // Lustrzane odbicie zwolnienia: tam kolejkujemy odebranie dostępu w Hubie, tu ponowne
        // zaproszenie. Kolejka sama pomija firmy nieobsługiwane przez Hub.
        await accessProvisioningQueue.QueueInvitationAsync(
            new EmployeeAccessInvitationRequest(
                employee.TenantId,
                employee.Id,
                employee.Email,
                employee.FirstName,
                employee.LastName),
            cancellationToken);

        return Result.Success();
    }
}
