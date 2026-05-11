using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

public sealed record SetEmployeeHourlyRateCommand(Guid Id, decimal? HourlyRate)
    : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class SetEmployeeHourlyRateHandler(IEmployeeRepository repository)
    : ICommandHandler<SetEmployeeHourlyRateCommand>
{
    public async Task<Result> Handle(SetEmployeeHourlyRateCommand request, CancellationToken cancellationToken)
    {
        var employee = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (employee is null || employee.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("Employee.NotFound", "Pracownik nie został znaleziony."));

        if (request.HourlyRate.HasValue && request.HourlyRate.Value < 0)
            return Result.Failure(Error.Validation("Employee.InvalidRate", "Stawka godzinowa nie może być ujemna."));

        employee.SetHourlyRate(request.HourlyRate);
        repository.Update(employee);
        return Result.Success();
    }
}
