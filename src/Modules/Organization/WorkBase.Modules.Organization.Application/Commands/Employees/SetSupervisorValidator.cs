using FluentValidation;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

public sealed class SetSupervisorValidator : AbstractValidator<SetSupervisorCommand>
{
    public SetSupervisorValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required.");

        RuleFor(x => x.SupervisorEmployeeId)
            .NotEmpty().WithMessage("Supervisor employee ID is required.");
    }
}
