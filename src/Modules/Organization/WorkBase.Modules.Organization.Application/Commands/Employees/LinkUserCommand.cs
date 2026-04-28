using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

public sealed record LinkUserCommand(
    Guid EmployeeId,
    Guid UserId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class LinkUserHandler(IEmployeeRepository repository)
    : ICommandHandler<LinkUserCommand>
{
    public async Task<Result> Handle(LinkUserCommand request, CancellationToken cancellationToken)
    {
        var employee = await repository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null || employee.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("Employee.NotFound", "Pracownik nie został znaleziony."));

        employee.LinkUser(request.UserId);
        repository.Update(employee);

        return Result.Success();
    }
}
