using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

public sealed record DeactivateEmployeeCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeactivateEmployeeHandler(IEmployeeRepository repository)
    : ICommandHandler<DeactivateEmployeeCommand>
{
    public async Task<Result> Handle(DeactivateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (employee is null || employee.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("Employee.NotFound", "Pracownik nie został znaleziony."));

        employee.Deactivate(DateTime.UtcNow);
        repository.Update(employee);
        return Result.Success();
    }
}
