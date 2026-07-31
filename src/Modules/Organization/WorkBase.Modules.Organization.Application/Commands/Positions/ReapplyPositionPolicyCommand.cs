using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Application.Services;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Positions;

/// <summary>
/// Stosuje polityke stanowisk do przypisan, ktore powstaly, zanim ta polityka istniala.
/// </summary>
/// <remarks>
/// Rola ze stanowiska i przelozenstwo w jednostce sa nadawane w momencie przypisania pracownika.
/// Instalacje, ktore skonfigurowaly stanowiska wczesniej, zostaly z pustymi relacjami przelozonych
/// i bez rol — a bez przelozonego nie da sie zlozyc wniosku urlopowego. To polecenie nadrabia to
/// jednym przebiegiem; jest idempotentne, wiec mozna je uruchomic ponownie bez skutkow ubocznych.
/// </remarks>
public sealed record ReapplyPositionPolicyCommand : ICommand<ReapplyPositionPolicyResult>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed record ReapplyPositionPolicyResult(int PrzetworzonychPrzypisan, int Pominietych);

public sealed class ReapplyPositionPolicyHandler(
    IEmployeeAssignmentRepository assignmentRepository,
    IEmployeeRepository employeeRepository,
    IPositionRepository positionRepository,
    PositionAssignmentPolicy policy)
    : ICommandHandler<ReapplyPositionPolicyCommand, ReapplyPositionPolicyResult>
{
    public async Task<Result<ReapplyPositionPolicyResult>> Handle(
        ReapplyPositionPolicyCommand request,
        CancellationToken cancellationToken)
    {
        var assignments = await assignmentRepository.GetActivePrimaryByTenantAsync(request.TenantId, cancellationToken);
        var positions = await positionRepository.GetAllByTenantAsync(request.TenantId, cancellationToken);
        var positionsById = positions.ToDictionary(position => position.Id);

        var przetworzone = 0;
        var pominiete = 0;

        // Najpierw stanowiska kierownicze: polityka dla zwyklego pracownika szuka kierownika
        // w jednostce, wiec bez tej kolejnosci pierwsi przetwarzani nie znalezliby jeszcze
        // nikogo i zostaliby bez przelozonego.
        foreach (var assignment in assignments
            .OrderByDescending(assignment => positionsById.TryGetValue(assignment.PositionId, out var position) && position.IsManagerial))
        {
            var employee = await employeeRepository.GetByIdAsync(assignment.EmployeeId, cancellationToken);
            if (employee is null || !positionsById.TryGetValue(assignment.PositionId, out var position))
            {
                pominiete++;
                continue;
            }

            await policy.ApplyAsync(employee, assignment.OrganizationUnitId, position, request.TenantId, cancellationToken);

            // Zapis po kazdym przypisaniu, bo polityka sprawdza w bazie, czy pracownik ma juz
            // przelozonego. Bez utrwalenia kolejne przebiegi nie widza relacji zalozonych przed
            // chwila i zakladaja duplikaty.
            await employeeRepository.SaveChangesAsync(cancellationToken);
            przetworzone++;
        }

        return new ReapplyPositionPolicyResult(przetworzone, pominiete);
    }
}
